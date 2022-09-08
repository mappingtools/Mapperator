using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapperator {
    public static class DataSerializer {
        public static IEnumerable<string> SerializeBeatmapData(IEnumerable<MapDataPoint> data) {
            return data.Select(SerializeBeatmapDataSample);
        }

        public static string SerializeBeatmapDataSample(MapDataPoint data) {
            return data.ToString();
        }

        public static IEnumerable<MapDataPoint> DeserializeBeatmapData(IEnumerable<string> data) {
            return data.Select(DeserializeBeatmapDataSample);
        }

        public static MapDataPoint DeserializeBeatmapDataSample(string data) {
            var split = data.Split(' ');
            return new MapDataPoint(
                (DataType)int.Parse(split[0]),
                double.Parse(split[1]),
                double.Parse(split[2]),
                double.Parse(split[3]),
                split[4] == "1",
                string.IsNullOrEmpty(split[5]) ? null : (PathType)int.Parse(split[5]),
                string.IsNullOrEmpty(split[6]) ? null : int.Parse(split[6]),
                split[7]
                );
        }
    }
}
