using Mapperator.Matching.DataStructures;
using Mapperator.Model;

namespace Mapperator.Matching.Matchers {
    public class TrieDataMatcher : IMinLengthProvider {
        private readonly RhythmDistanceTrieStructure data;
        private readonly ReadOnlyMemory<RhythmToken> patternRhythmString;

        public int MinLength { get; set; }

        public int Length => patternRhythmString.Length;

        public TrieDataMatcher(RhythmDistanceTrieStructure data, ReadOnlySpan<MapDataPoint> pattern) : this(data, RhythmDistanceTrieStructure.ToRhythmString(pattern)) { }

        public TrieDataMatcher(RhythmDistanceTrieStructure data, ReadOnlyMemory<RhythmToken> rhythmString) {
            this.data = data;
            patternRhythmString = rhythmString;
        }

        public IEnumerable<Match> FindMatches(int i) {
            var searchLength = patternRhythmString.Length - i;

            var query = patternRhythmString.Slice(i, searchLength);
            var result = data.Trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, this);

            // Yield all new matches
            foreach (var (w, length, minMult, maxMult) in result) {
                yield return new Match(data.Data[w.Value].AsMemory().Slice(w.CharPosition, length), w, minMult, maxMult);
            }
        }
    }
}