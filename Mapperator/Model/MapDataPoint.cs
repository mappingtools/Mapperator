using System.Globalization;
using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapperator.Model {
    public struct MapDataPoint {
        public DataType DataType;
        public double BeatsSince;
        public double Spacing;  // The distance from the previous to this point
        public double Angle;  // The angle between the vectors to the previous and previous previous points
        public bool NewCombo;  // Whether this is on a new combo, only applies to Hit types
        public PathType? SliderType;  // If on a slider hit, this shows the type of the slider
        public int? Repeats;  // The number of repeats on a slider
        public string? HitObject;  // The hit object data

        public MapDataPoint(DataType dataType, double beatsSince, double spacing, double angle, bool newCombo = false, PathType? sliderType = null, int? repeats = null, string? hitObject = null) {
            DataType = dataType;
            BeatsSince = beatsSince;
            Spacing = spacing;
            Angle = angle;
            NewCombo = newCombo;
            SliderType = sliderType;
            Repeats = repeats;
            HitObject = hitObject;
        }

        public override string ToString() {
            return $"{((int)DataType).ToString(CultureInfo.InvariantCulture)} {BeatsSince.ToString("N4", CultureInfo.InvariantCulture)} {Spacing.ToString("N0", CultureInfo.InvariantCulture)} {Angle.ToString("N4", CultureInfo.InvariantCulture)} {(NewCombo ? 1 : 0).ToString(CultureInfo.InvariantCulture)} {(SliderType.HasValue ? ((int)SliderType).ToString(CultureInfo.InvariantCulture) : string.Empty)} {(Repeats.HasValue ? Repeats.Value.ToString(CultureInfo.InvariantCulture) : string.Empty)} {HitObject}";
        }
    }
}
