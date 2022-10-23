using Mapperator.Model;

namespace Mapperator.Matching.Judges;

public class SuperJudge : IJudge {
    private const double LengthBonus = 50;
    private const double SpacingWeight = 100;
    private const double SliderLengthWeight = 2;
    private const double SliderSegmentWeight = 20;
    private const double AngleWeight = 1;
    private const double NcLoss = 5;
    private const double WeightDeviation = 2;
    private const double ExpectedMatchingCost = 10;  // Important parameter for speeding up search with early termination

    private readonly ReadOnlyMemory<MapDataPoint> pattern;

    public int PatternIndex { get; set; }

    public SuperJudge(ReadOnlyMemory<MapDataPoint> pattern) {
        this.pattern = pattern;
    }

    public double Judge(Match match) {
        double score = 0;
        var mult = match.MinMult == 0 && double.IsPositiveInfinity(match.MaxMult) ? 1 : Math.Sqrt(match.MinMult * match.MaxMult);

        for (var i = 0; i < match.Sequence.Length; i++) {
            var foundPoint = match.Sequence.Span[i];
            var wantedPoint = pattern.Span[PatternIndex + i];

            // Get a weight factor using the gaussian formula
            var weight = Math.Exp(-(i * i) / (2 * WeightDeviation * WeightDeviation));

            // Apply match length bonus
            score += weight * LengthBonus;

            // Subtract loss by difference in data points
            score -= weight * SpacingWeight * Math.Abs(foundPoint.Spacing * mult - wantedPoint.Spacing) / (wantedPoint.Spacing + 2);
            score -= weight * (wantedPoint.Spacing < 100 && wantedPoint.BeatsSince < 4.9 ? 20 : 1) * AngleWeight * Math.Abs(Math.Abs(foundPoint.Angle) - Math.Abs(wantedPoint.Angle));

            // Subtract points for having different combo's
            score -= weight * (foundPoint.NewCombo != wantedPoint.NewCombo ? NcLoss : 0);

            // Subtract score for mismatching slider length and segment count
            if (foundPoint.SliderLength.HasValue && wantedPoint.SliderLength.HasValue &&
                foundPoint.SliderSegments.HasValue && wantedPoint.SliderSegments.HasValue) {
                score -= weight * SliderLengthWeight * Math.Pow(Math.Abs(foundPoint.SliderLength.Value * mult - wantedPoint.SliderLength.Value), 0.5);
                // Compare segments per pixel
                var foundSegmentRatio = Math.Log2(Math.Min(.5, foundPoint.SliderSegments.Value / foundPoint.SliderLength.Value));
                var wantedSegmentRatio = Math.Log2(Math.Min(.5, wantedPoint.SliderSegments.Value / wantedPoint.SliderLength.Value));
                score -= weight * SliderSegmentWeight * Math.Pow(Math.Abs(foundSegmentRatio - wantedSegmentRatio), 2);
            }
        }

        return score;
    }

    public int MinLengthForScore(double wantedScore) {
        double score = 0;
        var length = 0;

        while (score < wantedScore && length < 32) {
            // Get a weight factor using the gaussian formula
            var weight = Math.Exp(-(length * length) / (2 * WeightDeviation * WeightDeviation));

            // Apply match length bonus
            // Add extra term for pessimism
            score += weight * (LengthBonus - ExpectedMatchingCost);
            length++;
        }

        return length;
    }

    public double PogScore() {
        return 80;
    }
}