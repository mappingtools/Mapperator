using System.Globalization;

namespace Mapperator.Model {
    public struct MapDataPoint2 {
        public DataType2 DataType;
        public int TimeSince;  // The number of beats since the last data point
        public double Spacing;  // The distance from the previous to this point
        public double Angle;  // The angle between the vectors to the previous and previous previous points

        public MapDataPoint2(DataType2 dataType, int timeSince, double spacing, double angle) {
            DataType = dataType;
            TimeSince = timeSince;
            Spacing = spacing;
            Angle = angle;
        }

        public override string ToString() {
            return $"{((int)DataType).ToString(CultureInfo.InvariantCulture)} {TimeSince.ToString(CultureInfo.InvariantCulture)} {Spacing.ToString("N0", CultureInfo.InvariantCulture)} {Angle.ToString("N4", CultureInfo.InvariantCulture)}";
        }
    }
}
