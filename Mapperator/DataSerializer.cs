using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using System.Collections.Generic;

namespace Mapperator {
    public static class DataSerializer {
        public static IEnumerable<string> SerializeBeatmapData(IEnumerable<MapDataPoint> data) {
            foreach (var d in data) {
                yield return SerializeBeatmapDataSample(d);
            }
        }

        public static string SerializeBeatmapDataSample(MapDataPoint data) {
            return $"{(int)data.DataType} {data.BeatsSince:N4} {data.Spacing:N0} {data.Angle:N4} {(data.NewCombo ? 1 : 0)} {(data.SliderType.HasValue ? (int) data.SliderType : string.Empty)} {data.Repeats} {data.HitObject}";
        }

        public static IEnumerable<MapDataPoint> DeserializeBeatmapData(IEnumerable<string> data) {
            foreach (var d in data) {
                yield return DeserializeBeatmapDataSample(d);
            }
        }

        public static MapDataPoint DeserializeBeatmapDataSample(string data) {
            var split = data.Split(' ', System.StringSplitOptions.None);
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
