using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching.Matchers {
    public class SimpleDataMatcher : IDataMatcher {
        private readonly List<MapDataPoint> mapDataPoints = new();

        private readonly double[] weights = { 2, 4, 9, 16, 9, 4, 2 };

        public void AddData(IEnumerable<MapDataPoint> data) {
            mapDataPoints.AddRange(data);
        }

        public IEnumerable<MapDataPoint> FindSimilarData(ReadOnlyMemory<MapDataPoint> pattern, Func<MapDataPoint, bool> isValidFunc) {
            for (var i = 0; i < pattern.Length; i++) {
                yield return FindBestMatch(pattern.Span, i, isValidFunc);
            }
        }

        public MapDataPoint FindBestMatch(ReadOnlySpan<MapDataPoint> pattern, int i, Func<MapDataPoint, bool> isValidFunc) {
            // Find the element of mapDataPoints which is locally the most similar to pattern at i

            // Normalize the weights for this offset
            const int mid = 3;  // Middle index of the kernel
            var lm = Math.Min(mid, i);  // Left index of the kernel
            var rm = Math.Min(weights.Length - mid, pattern.Length - i) - 1;  // Right index of the kernel
            double s = 0;
            for (var k = mid - lm; k < mid + rm; k++) {
                s += weights[k];
            }
            var normalizedWeights = weights.Select(o => o / s).ToArray();  // Normalized weights

            var bestLoss = double.PositiveInfinity;
            var bestPoint = 0;
            for (var j = lm; j < mapDataPoints.Count - rm; j++) {
                double loss = 0;
                for (var k = j - lm; k < j + rm; k++) {
                    var w = normalizedWeights[k - j + mid];
                    loss += w * ComputeLoss(mapDataPoints[k], pattern[k - j + i]);
                }

                if (loss < bestLoss && isValidFunc(mapDataPoints[j])) {
                    bestLoss = loss;
                    bestPoint = j;
                }
            }

            return mapDataPoints[bestPoint];
        }

        private static double ComputeLoss(MapDataPoint tp, MapDataPoint pp) {
            double typeLoss = tp.DataType == pp.DataType ? 0 : 100;
            var beatsLoss = 100 * Math.Sqrt(Math.Abs(Math.Min(tp.BeatsSince, 2) - Math.Min(pp.BeatsSince, 2)));  // Non-slider gaps bigger than 2 beats are mostly equal
            var spacingLoss = tp.DataType == DataType.Release && pp.DataType == DataType.Release ?
                4 * Math.Sqrt(Math.Abs(tp.Spacing - pp.Spacing)) :
                2 * Math.Sqrt(Math.Abs(tp.Spacing - pp.Spacing));
            var angleLoss = 1 * Math.Min(Helpers.Mod(tp.Angle - pp.Angle, MathHelper.TwoPi), Helpers.Mod(pp.Angle - tp.Angle, MathHelper.TwoPi));
            double sliderLoss = tp.SliderType == pp.SliderType ? 0 : 10;
            return typeLoss + beatsLoss + spacingLoss + angleLoss + sliderLoss;
        }
    }
}
