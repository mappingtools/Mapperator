using HNSW.Net;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using System.Collections.Generic;
using System.IO;

namespace Mapperator {
    public static class DataSerializer {
        public static IEnumerable<string> SerializeBeatmapData(IEnumerable<MapDataPoint> data) {
            foreach (var d in data) {
                yield return SerializeBeatmapDataSample(d);
            }
        }

        public static string SerializeBeatmapDataSample(MapDataPoint data) {
            return $"{(int)data.DataType} {data.BeatsSince:N4} {data.Spacing:N0} {data.Angle:N4} {(data.SliderType.HasValue ? (int) data.SliderType : string.Empty)} {data.HitObject}";
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
                string.IsNullOrEmpty(split[4]) ? null : (PathType)int.Parse(split[4]),
                split[5]
                );
        }
    }
}
