using Mapperator.Model;

namespace Mapperator.Matching {
    public interface IDataMatcher {
        IEnumerable<(MapDataPoint, double)> FindSimilarData(Func<MapDataPoint, bool> isValidFunc);

        (MapDataPoint, double) FindBestMatch(int i, Func<MapDataPoint, bool> isValidFunc);
    }
}
