using Mapperator.Model;

namespace Mapperator.Matching {
    public interface IDataMatcher {
        IEnumerable<MapDataPoint> FindSimilarData(Func<MapDataPoint, bool> isValidFunc);

        MapDataPoint FindBestMatch(int i, Func<MapDataPoint, bool> isValidFunc);
    }
}
