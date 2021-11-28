using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapperator.Model {
    public struct MapDataPoint {
        public DataType DataType;
        public double Spacing;  // The distance from the previous to this point
        public double Angle;  // The angle between the vectors to the previous and next points
        public PathType SliderType;  // If on a slider hit, this shows the type of the slider
    }
}
