using System.Globalization;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.Sections;
using MoreLinq;

namespace Mapperator {
    public static class DataSerializer2 {
        private const string BeatmapSeparator = "/-\\_/-\\_/-\\";

        public static IEnumerable<string> SerializeBeatmapData(IEnumerable<(IEnumerable<MapDataPoint2>, SectionDifficulty, int)> data) {
            foreach (var (beatmap, difficulty, beatmapId) in data) {
                yield return beatmapId.ToString(CultureInfo.InvariantCulture);
                yield return difficulty.ApproachRate.ToString(CultureInfo.InvariantCulture);
                yield return difficulty.CircleSize.ToString(CultureInfo.InvariantCulture);
                yield return difficulty.OverallDifficulty.ToString(CultureInfo.InvariantCulture);

                foreach (var dataPoint in beatmap) {
                    yield return SerializeBeatmapDataSample(dataPoint);
                }

                yield return BeatmapSeparator;
            }
        }

        public static string SerializeBeatmapDataSample(MapDataPoint2 data) {
            return data.ToString();
        }

        public static IEnumerable<IEnumerable<MapDataPoint2>> DeserializeBeatmapData(IEnumerable<string> data) {
            return data.Split(BeatmapSeparator, beatmapData => beatmapData.Select(DeserializeBeatmapDataSample));
        }

        public static MapDataPoint2 DeserializeBeatmapDataSample(string data) {
            var split = data.Split(' ');
            return new MapDataPoint2(
                (DataType2)int.Parse(split[0], CultureInfo.InvariantCulture),
                int.Parse(split[1], CultureInfo.InvariantCulture),
                double.Parse(split[2], CultureInfo.InvariantCulture),
                double.Parse(split[3], CultureInfo.InvariantCulture)
                );
        }
    }
}
