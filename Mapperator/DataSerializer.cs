using System.Globalization;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using MoreLinq;

namespace Mapperator {
    public static class DataSerializer {
        private const string BeatmapSeparator = "/-\\_/-\\_/-\\";

        public static IEnumerable<string> SerializeBeatmapData(IEnumerable<IEnumerable<MapDataPoint>> data) {
            foreach (var beatmap in data) {
                foreach (var dataPoint in beatmap) {
                    yield return SerializeBeatmapDataSample(dataPoint);
                }

                yield return BeatmapSeparator;
            }
        }

        public static string SerializeBeatmapDataSample(MapDataPoint data) {
            return data.ToString();
        }

        public static IEnumerable<IEnumerable<MapDataPoint>> DeserializeBeatmapData(IEnumerable<string> data) {
            return data.Split(BeatmapSeparator, beatmapData => beatmapData.Select(DeserializeBeatmapDataSample));
        }

        public static MapDataPoint DeserializeBeatmapDataSample(string data) {
            var split = data.Split(' ');
            return new MapDataPoint(
                (DataType)int.Parse(split[0], CultureInfo.InvariantCulture),
                double.Parse(split[1], CultureInfo.InvariantCulture),
                double.Parse(split[2], CultureInfo.InvariantCulture),
                double.Parse(split[3], CultureInfo.InvariantCulture),
                split[4] == "1",
                string.IsNullOrEmpty(split[5]) ? null : (PathType)int.Parse(split[5], CultureInfo.InvariantCulture),
                string.IsNullOrEmpty(split[6]) ? null : int.Parse(split[6], CultureInfo.InvariantCulture),
                split[7]
                );
        }
    }
}
