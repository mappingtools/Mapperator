using Mapperator.Model;

namespace Mapperator.Matching {
    public interface IDataMatcher {
        void AddData(IEnumerable<MapDataPoint> data);

        IEnumerable<MapDataPoint> FindSimilarData(ReadOnlyMemory<MapDataPoint> pattern, Func<MapDataPoint, bool> isValidFunc);

        MapDataPoint FindBestMatch(ReadOnlySpan<MapDataPoint> pattern, int i, Func<MapDataPoint, bool> isValidFunc);
    }
}
