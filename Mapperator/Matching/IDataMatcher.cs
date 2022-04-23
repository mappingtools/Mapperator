using Mapperator.Model;
using System;
using System.Collections.Generic;

namespace Mapperator.Matching {
    public interface IDataMatcher {
        void AddData(IEnumerable<MapDataPoint> data);

        IEnumerable<MapDataPoint> FindSimilarData(IReadOnlyList<MapDataPoint> pattern, Func<MapDataPoint, bool> isValidFunc = null);

        MapDataPoint FindBestMatch(IReadOnlyList<MapDataPoint> pattern, int i, Func<MapDataPoint, bool> isValidFunc = null);
    }
}
