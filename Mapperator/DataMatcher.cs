using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools_Core.BeatmapHelper.IO.Encoding.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;
using Mapping_Tools_Core.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mapperator {
    public static class DataMatcher {
        public static IEnumerable<MapDataPoint> FindSimilarData(IReadOnlyList<MapDataPoint> trainData, IReadOnlyList<MapDataPoint> pattern) {
            var tasks = new List<Task<MapDataPoint>>();
            for (int i = 0; i < pattern.Count; i++) {
                int i2 = i;
                tasks.Add(Task.Run(() => {
                    return FindBestMatch(trainData, pattern, i2);
                }));
            }
            Task t = Task.WhenAll(tasks);
            t.Wait();
            foreach (var task in tasks) {
                yield return task.Result;
            }
        }

        private static MapDataPoint FindBestMatch(IReadOnlyList<MapDataPoint> trainData, IReadOnlyList<MapDataPoint> pattern, int i) {
            // Find the element of trainData which is locally the most similar to pattern at i

            // Normalize the weights for this offset
            const int mid = 8;  // Middle index of the kernel
            int lm = Math.Min(mid, i);  // Left index of the kernel
            int rm = Math.Min(weights.Length - mid, pattern.Count - i) - 1;  // Right index of the kernel
            int l = lm + rm + 1;  // Length of the kernel
            double s = 0;
            for (int k = mid - lm; k < mid + rm; k++) {
                s += weights[k];
            }
            double[] normalizedWeights = weights.Select(o => o / s).ToArray();  // Normalized weights

            double bestLoss = double.PositiveInfinity;
            int bestPoint = 0;
            for (int j = lm; j < trainData.Count - rm; j++) {
                double loss = 0;
                for (int k = j - lm; k < j + rm; k++) {
                    var w = normalizedWeights[k - j + mid];
                    loss += w * ComputeLoss(trainData[k], pattern[k - j + i]);
                }

                if (loss < bestLoss) {
                    bestLoss = loss;
                    bestPoint = j;
                }
            }

            return trainData[bestPoint];
        }

        private static double ComputeLoss(MapDataPoint tp, MapDataPoint pp) {
            double typeLoss = tp.DataType == pp.DataType ? 0 : 100;
            double beatsLoss = 100 * Math.Abs(tp.BeatsSince - pp.BeatsSince);
            double spacingLoss = 2 * Math.Abs(tp.Spacing - pp.Spacing);
            double angleLoss = 1 * Math.Min(Helpers.Mod(tp.Angle - pp.Angle, MathHelper.TwoPi), Helpers.Mod(pp.Angle - tp.Angle, MathHelper.TwoPi));
            double sliderLoss = tp.SliderType == pp.SliderType ? 0 : 1;
            return typeLoss + beatsLoss + spacingLoss + angleLoss + sliderLoss;
        }

        private static readonly double[] weights = new double[] { 0.125, 0.25, 0.5, 1, 2, 4, 9, 16, 9, 4, 2, 1 };
    }
}
