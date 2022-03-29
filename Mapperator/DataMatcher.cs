using HNSW.Net;
using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mapperator {
    public class DataMatcher {
        private class ConsoleProgressReporter : IProgressReporter {
            public void Progress(int current, int total) {
                if (current % 1000 == 0 || current == total)
                    Console.WriteLine($"Progress: {current}/{total}");
            }
        }

        private readonly double[] weightsNormalized;
        private readonly double[] weights = new double[] { 4, 9, 16, 9, 4 };
        private readonly double[] weightsSums = new double[] { 16, 25, 34, 38, 42, 44, 46, 47, 48 };
        private readonly int weightsMiddle = 2;

        public DataMatcher() {
            double s = weights.Sum();
            weightsNormalized = weights.Select(o => o / s).ToArray();  // Normalized weights
        }

        public IEnumerable<MapDataPoint> FindSimilarData(IReadOnlyList<MapDataPoint> trainData, IReadOnlyList<MapDataPoint> pattern) {
            for (int i = 0; i < pattern.Count; i++) {
                yield return FindBestMatch(trainData, pattern, i);
            }
        }

        public IEnumerable<MapDataPoint> FindSimilarData2(SmallWorld<MapDataPoint[], double> graph, IReadOnlyList<MapDataPoint> pattern, Func<MapDataPoint, bool> isValidFunc = null) {
            Console.WriteLine("Searching for matches");
            // We want to replace the previous parts of the pattern with the matches we found so the next matches have a better chance
            // of continuing the previous pattern
            MapDataPoint[] newPattern = pattern.ToArray();
            int lastId = -1;
            int pogs = 0;
            for (int i = 0; i < pattern.Count; i++) {
                var match = FindBestMatch2(graph, newPattern, i, ref lastId, ref pogs, isValidFunc);
                newPattern[i] = match;
                yield return match;
            }
            Console.WriteLine($"Pograte = {(float)pogs / pattern.Count}");
        }

        private MapDataPoint FindBestMatch2(SmallWorld<MapDataPoint[], double> graph, IReadOnlyList<MapDataPoint> pattern, int i, ref int lastId, ref int pogs, Func<MapDataPoint, bool> isValidFunc = null) {
            const int tries = 200;
            var result = graph.KNNSearch(GetNeighborhood(pattern, i), isValidFunc is null ? 1 : tries);
            var bestLoss = result[0].Distance;
            if (lastId != -1) {
                foreach (var r in result) {
                    if (r.Id == lastId + 1 && r.Distance <= bestLoss * 2) {
                        lastId = r.Id;
                        var best = r.Item[r.Item.Length / 2];
                        Console.WriteLine($"POGGERS match {i}, loss = {r.Distance}, type = {best.DataType}, id = {r.Id}");
                        pogs++;
                        return best;
                    } 
                }
            }
            
            for (int j = 0; j < result.Count; j++) {
                var bestGroup = result[j];
                var best = bestGroup.Item[bestGroup.Item.Length / 2];
                if (isValidFunc is null || isValidFunc(best)) {
                    Console.WriteLine($"Match {i}, loss = {bestGroup.Distance}, type = {best.DataType}, id = {bestGroup.Id}");
                    lastId = bestGroup.Id;
                    return best;
                }
            }
            lastId = result[0].Id;
            return result[0].Item[result[0].Item.Length / 2];
        }

        private MapDataPoint FindBestMatch(IReadOnlyList<MapDataPoint> trainData, IReadOnlyList<MapDataPoint> pattern, int i) {
            // Find the element of trainData which is locally the most similar to pattern at i

            // Normalize the weights for this offset
            const int mid = 4;  // Middle index of the kernel
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
            // TODO: Disqualify patterns which would result in current or future objects outside of the mapping bounding box
            // TODO: Use topological data structures to decrease searching time hopefully to log(N)

            return trainData[bestPoint];
        }

        private List<MapDataPoint[]> FoldData(IReadOnlyList<MapDataPoint> data) {
            List<MapDataPoint[]> foldedData = new List<MapDataPoint[]>(data.Count);
            for (int i = 0; i < data.Count; i++) {
                foldedData.Add(GetNeighborhood(data, i));
            }
            return foldedData;
        }

        private MapDataPoint[] GetNeighborhood(IReadOnlyList<MapDataPoint> data, int i) {
            int lm = Math.Min(weightsMiddle, i);  // Left index of the kernel
            int rm = Math.Min(weights.Length - weightsMiddle, data.Count - i) - 1;  // Right index of the kernel
            lm = Math.Min(lm, rm + 1);
            rm = Math.Min(rm, lm);
            int l = lm + rm + 1;  // Length of the kernel

            MapDataPoint[] dataPoints = new MapDataPoint[l];
            for (int k = 0; k < l; k++) {
                dataPoints[k] = data[i + k - lm];
            }
            return dataPoints;
        }

        private double WeightedComputeLoss(MapDataPoint[] tp, MapDataPoint[] pp) {
            int l = Math.Min(tp.Length, pp.Length);
            int tOffset = Math.Max((tp.Length - pp.Length) / 2, 0);
            int pOffset = Math.Max((pp.Length - tp.Length) / 2, 0);
            int weightOffset = (weights.Length - l) / 2;
            double loss = 0;
            for (int k = 0; k < l; k++) {
                var w = weights[k + weightOffset] / weightsSums[l - 1];
                loss += w * ComputeLoss(tp[k + tOffset], pp[k + pOffset]);
            }
            return loss;
        }

        private static double ComputeLoss(MapDataPoint tp, MapDataPoint pp) {
            double typeLoss = tp.DataType == pp.DataType ? 0 : 50;
            //double beatsLoss = 100 * Math.Abs(Math.Min(tp.BeatsSince, 2) - Math.Min(pp.BeatsSince, 2));  // Gaps bigger than 2 beats are mostly equal
            double beatsLoss = 100 * Math.Sqrt(Math.Abs(tp.BeatsSince - pp.BeatsSince));
            double spacingLoss = 2 * Math.Sqrt(Math.Abs(tp.Spacing - pp.Spacing));
            double angleLoss = 1 * Math.Min(Helpers.Mod(tp.Angle - pp.Angle, MathHelper.TwoPi), Helpers.Mod(pp.Angle - tp.Angle, MathHelper.TwoPi));
            double sliderLoss = tp.SliderType == pp.SliderType ? 0 : 1;
            return typeLoss + beatsLoss + spacingLoss + angleLoss + sliderLoss;
        }

        #region Graph Creation

        public SmallWorld<MapDataPoint[], double> CreateGraph(IReadOnlyList<MapDataPoint> data) {
            Console.WriteLine("Folding data...");
            var foldedData = FoldData(data);

            Console.WriteLine("Starting graph creation...");
            var parameters = new SmallWorld<MapDataPoint[], double>.Parameters() {
                M = 32,
                LevelLambda = 1 / Math.Log(32),
            };

            var graph = new SmallWorld<MapDataPoint[], double>(WeightedComputeLoss, DefaultRandomGenerator.Instance, parameters);
            graph.AddItems(foldedData, new ConsoleProgressReporter());

            return graph;
        }

        public void SaveGraph(SmallWorld<MapDataPoint[], double> graph, string path) {
            using Stream file = File.Create(path);
            graph.SerializeGraph(file);
        }

        public SmallWorld<MapDataPoint[], double> LoadGraph(IReadOnlyList<MapDataPoint> data, string path) {
            Console.WriteLine("Folding data...");
            var foldedData = FoldData(data);
            Console.WriteLine("Loading graph from file...");
            using Stream file = File.OpenRead(path);
            return SmallWorld<MapDataPoint[], double>.DeserializeGraph(foldedData, WeightedComputeLoss, DefaultRandomGenerator.Instance, file);
        }

        #endregion
    }
}
