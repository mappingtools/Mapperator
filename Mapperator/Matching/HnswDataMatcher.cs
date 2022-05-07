using HNSW.Net;
using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching {
    public class HnswDataMatcher : IDataMatcher, ISerializable {
        private class ConsoleProgressReporter : IProgressReporter {
            public void Progress(int current, int total) {
                if (current % 1000 == 0 || current == total)
                    Console.WriteLine($"Progress: {current}/{total}");
            }
        }

        private SmallWorld<MapDataPoint[], double> graph;
        private IReadOnlyList<MapDataPoint[]> points;
        private int lastId;
        private int pogs;

        private readonly double[] weights = { 4, 9, 16, 9, 4 };
        private readonly double[] weightsSums = { 16, 25, 34, 38, 42, 44, 46, 47, 48 };
        private const int WeightsMiddle = 2;

        public HnswDataMatcher() {
            var parameters = new SmallWorld<MapDataPoint[], double>.Parameters() {
                M = 32,
                LevelLambda = 1 / Math.Log(32),
            };

            graph = new SmallWorld<MapDataPoint[], double>(WeightedComputeLoss, DefaultRandomGenerator.Instance, parameters);
            points = graph.Items;
        }

        public void AddData(IEnumerable<MapDataPoint> data) {
            Console.WriteLine("Folding data...");
            var foldedData = FoldData(data.ToList());

            Console.WriteLine("Adding items to graph...");
            
            graph.AddItems(foldedData, new ConsoleProgressReporter());
            points = graph.Items;
        }

        public IEnumerable<MapDataPoint> FindSimilarData(IReadOnlyList<MapDataPoint> pattern, Func<MapDataPoint, bool> isValidFunc) {
            Console.WriteLine("Searching for matches");
            // We want to replace the previous parts of the pattern with the matches we found so the next matches have a better chance
            // of continuing the previous pattern
            var newPattern = pattern.ToArray();
            lastId = -1;
            pogs = 0;
            for (var i = 0; i < pattern.Count; i++) {
                var match = FindBestMatch(newPattern, i, isValidFunc);
                newPattern[i] = match;
                yield return match;
            }
            Console.WriteLine($"Pograte = {(float)pogs / pattern.Count}");
        }

        public MapDataPoint FindBestMatch(IReadOnlyList<MapDataPoint> pattern, int i, Func<MapDataPoint, bool> isValidFunc) {
            const int tries = 200;
            var result = graph.KNNSearch(GetNeighborhood(pattern, i), tries);

            // Try to use the next ID instead of the result
            var bDist = result[0].Distance;
            if (lastId != -1 && lastId + 1 < points.Count) {
                var nBestGroup = points[lastId + 1];
                var nBest = nBestGroup[nBestGroup.Length / 2];
                var nDist = WeightedComputeLoss(nBestGroup, GetNeighborhood(pattern, i));

                if ((isValidFunc(nBest)) && nDist <= bDist * 2 && nDist < 100) {
                    lastId++;
                    pogs++;
                    Console.WriteLine($"POGGERS match {i}, type = {nBest.DataType}, id = {lastId}, loss = {nDist}");
                    return nBest;
                }
            }

            foreach (var bestGroup in result) {
                var best = bestGroup.Item[bestGroup.Item.Length / 2];
                if (!isValidFunc(best)) continue;
                Console.WriteLine($"Match {i}, type = {best.DataType}, id = {bestGroup.Id}, loss = {bestGroup.Distance}");
                lastId = bestGroup.Id;
                return best;
            }
            lastId = result[0].Id;
            return result[0].Item[result[0].Item.Length / 2];
        }

        private List<MapDataPoint[]> FoldData(IReadOnlyList<MapDataPoint> data) {
            var foldedData = new List<MapDataPoint[]>(data.Count);
            foldedData.AddRange(data.Select((_, i) => GetNeighborhood(data, i)));
            return foldedData;
        }

        private MapDataPoint[] GetNeighborhood(IReadOnlyList<MapDataPoint> data, int i) {
            var lm = Math.Min(WeightsMiddle, i);  // Left index of the kernel
            var rm = Math.Min(weights.Length - WeightsMiddle, data.Count - i) - 1;  // Right index of the kernel
            lm = Math.Min(lm, rm + 1);
            rm = Math.Min(rm, lm);
            var l = lm + rm + 1;  // Length of the kernel

            var dataPoints = new MapDataPoint[l];
            for (var k = 0; k < l; k++) {
                dataPoints[k] = data[i + k - lm];
            }
            return dataPoints;
        }

        private double WeightedComputeLoss(MapDataPoint[] tp, MapDataPoint[] pp) {
            var l = Math.Min(tp.Length, pp.Length);
            var tOffset = Math.Max((tp.Length - pp.Length) / 2, 0);
            var pOffset = Math.Max((pp.Length - tp.Length) / 2, 0);
            var weightOffset = (weights.Length - l) / 2;
            double loss = 0;
            for (var k = 0; k < l; k++) {
                var w = weights[k + weightOffset] / weightsSums[l - 1];
                loss += w * ComputeLoss(tp[k + tOffset], pp[k + pOffset]);
            }
            return loss;
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

        #region Graph Serialization

        public string DefaultExtension => ".hnsw";

        public void Save(Stream stream) {
            graph.SerializeGraph(stream);
        }

        public void Load(IEnumerable<MapDataPoint> data, Stream stream) {
            Console.WriteLine("Folding data...");
            var foldedData = FoldData(data.ToList());

            Console.WriteLine("Loading graph from file...");
            graph = SmallWorld<MapDataPoint[], double>.DeserializeGraph(foldedData, WeightedComputeLoss, DefaultRandomGenerator.Instance, stream);
            points = graph.Items;
        }

        #endregion
    }
}
