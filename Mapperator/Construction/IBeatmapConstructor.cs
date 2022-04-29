using System.Collections.Generic;
using Mapperator.Matching;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper;

namespace Mapperator.Construction {
    public interface IBeatmapConstructor {
        void PopulateBeatmap(IBeatmap beatmap, IReadOnlyList<MapDataPoint> input, IDataMatcher matcher);
    }
}