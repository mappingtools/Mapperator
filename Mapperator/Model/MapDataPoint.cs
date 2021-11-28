using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapperator.Model {
    public struct MapDataPoint {
        public DataType DataType;
        public double BeatsSince;
        public double Spacing;  // The distance from the previous to this point
        public double Angle;  // The angle between the vectors to the previous and previous previous points
        public PathType? SliderType;  // If on a slider hit, this shows the type of the slider

        public MapDataPoint(DataType dataType, double beatsSince, double spacing, double angle, PathType? sliderType) {
            DataType = dataType;
            BeatsSince = beatsSince;
            Spacing = spacing;
            Angle = angle;
            SliderType = sliderType;
        }

        public MapDataPoint(DataType dataType, double beatsSince, double spacing, double angle) : this(dataType, beatsSince, spacing, angle, null) { }
    }
}
