using Mapperator.Model;
using System.Collections.Generic;

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
    }
}
