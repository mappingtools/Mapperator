using Gma.DataStructures.StringSearch;

namespace Mapperator.Matching.DataStructures;

public class RhythmDistanceTrie : UkkonenTrie<RhythmToken, int> {
    public RhythmDistanceTrie() : base(1) { }

    public IEnumerable<WordPosition<int>> SearchCustom() {
        throw new NotImplementedException();
    }

}