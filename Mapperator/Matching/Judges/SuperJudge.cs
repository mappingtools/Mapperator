using Mapperator.Model;

namespace Mapperator.Matching.Judges;

public class SuperJudge : IJudge {
    private const double LengthBonus = 50;
    private const double SpacingWeight = 10;
    private const double AngleWeight = 5;
    private const double NcLoss = 25;
    private const double WeightDeviation = 2;

    public double Judge(ReadOnlySpan<MapDataPoint> foundPattern, ReadOnlySpan<MapDataPoint> wantedPattern, int lookBack) {
        double score = 0;

        for (var i = -lookBack; i < wantedPattern.Length - lookBack; i++) {
            var foundPoint = foundPattern[i + lookBack];
            var wantedPoint = wantedPattern[i + lookBack];

            // Get a weight factor using the gaussian formula
            var weight = Math.Exp(-(i * i) / (2 * WeightDeviation * WeightDeviation));

            // Apply match length bonus
            score += weight * LengthBonus;

            // Subtract loss by difference in data points
            score -= weight * SpacingWeight * Math.Pow(Math.Abs(foundPoint.Spacing - wantedPoint.Spacing), 0.5);
            score -= weight * AngleWeight * Math.Abs(foundPoint.Angle - wantedPoint.Angle);

            // Subtract points for having different combo's
            score -= weight * (foundPoint.NewCombo != wantedPoint.NewCombo ? NcLoss : 0);
        }

        return score;
    }

    public double BestPossibleScore(int length, int lookBack) {
        double score = 0;

        for (var i = -lookBack; i < length - lookBack; i++) {
            // Get a weight factor using the gaussian formula
            var weight = Math.Exp(-(i * i) / (2 * WeightDeviation * WeightDeviation));

            // Apply match length bonus
            score += weight * LengthBonus;
        }

        return score;
    }
}