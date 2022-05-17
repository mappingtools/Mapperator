using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching.Judges;

public class SuperJudge : IJudge {
    private const double LengthBonus = 50;
    private const double SpacingWeight = 2;
    private const double AngleWeight = 1;
    private const double NcLoss = 5;
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

    private const double SpacingWeight2 = 10;
    private const double AngleWeight2 = 5;
    private const double NcLoss2 = 25;

    public double MatchingCost(MapDataPoint expected, MapDataPoint actual) {
        var score = 0d;
        // Subtract loss by difference in data points
        score += SpacingWeight2 * Math.Pow(Math.Abs(actual.Spacing - expected.Spacing), 0.5);
        score += AngleWeight2 * Math.Abs(actual.Angle - expected.Angle);

        // Subtract points for having different combo's
        score += actual.NewCombo != expected.NewCombo ? NcLoss2 : 0;

        return score;
    }

    private const double TimeFactor = 0.8;
    private const double NcFactor = 0.8;
    private const double StrongRelationScore = 2;
    private const double WeakRelationScore = 1;

    public double RelationScore(ReadOnlySpan<MapDataPoint> expected, ReadOnlySpan<MapDataPoint> actual, int i, int j,
        double maxDiff) {
        var score = 0d;
        var weight = 1d;
        var stillSameSequence = true;
        var next = expected[i];
        while (--i >= 0 && --j >= 0) {
            // __xxooMoo
            // M = next
            // o = curr
            var diff = MatchingCost(expected[i], actual[j]);
            if (diff > maxDiff) {
                break;
            }
            var curr = expected[i];
            var weightfactor = Math.Pow(TimeFactor, next.BeatsSince) * (next.NewCombo ? NcFactor : 1);
            weight *= weightfactor;
            if (stillSameSequence && diff < Precision.DOUBLE_EPSILON) {
                // Same sequence: strong relation
                score += weight * StrongRelationScore;
            } else {
                // Different sequence: weak relation
                score += weight * WeakRelationScore;
                stillSameSequence = false;
            }
            next = curr;
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