using Mapperator.Matching.DataStructures;
using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching.Matchers {
    public class TrieDataMatcher2 : IDataMatcher2 {
        private const int FirstSearchLength = 32;
        //private static readonly int[] DistanceRanges = { 0, 3, 9 };  // Best values found by trial-and-error
        private static readonly int[] DistanceRanges = { 3 };  // Best values found by trial-and-error
        private const int MaxLookBack = 8;

        private readonly RhythmDistanceTrieStructure data;
        private readonly (ReadOnlyMemory<ushort>, ReadOnlyMemory<ushort>)[] patternRhythmString;

        private readonly HashSet<(int, int)> foundMatches = new();

        public TrieDataMatcher2(RhythmDistanceTrieStructure data, ReadOnlySpan<MapDataPoint> pattern) : this(data, RhythmDistanceTrieStructure.ToRhythmString(pattern).Span) { }

        public TrieDataMatcher2(RhythmDistanceTrieStructure data, ReadOnlySpan<ushort> rhythmString) {
            this.data = data;
            patternRhythmString = RhythmDistanceTrieStructure.ToDistanceRanges(rhythmString, DistanceRanges);
        }

        public IEnumerable<Match> FindMatches(int i) {
            foundMatches.Clear();
            var patternLength = patternRhythmString[0].Item1.Length;
            var searchLength = Math.Min(FirstSearchLength, patternLength - i);

            while (searchLength > 0) {
                var lookBack = GetLookBack(i, searchLength, patternLength);

                for (var j = 0; j < patternRhythmString.Length; j++) {
                    var min = patternRhythmString[j].Item1.Slice(i - lookBack, searchLength);
                    var max = patternRhythmString[j].Item2.Slice(i - lookBack, searchLength);

                    var result = data.Trie.RetrieveSubstringsRange(min, max);

                    // Yield all new matches
                    foreach (var w in result) {
                        var id = (w.Value, w.CharPosition + lookBack);

                        // Skip if this ID was already found
                        if (foundMatches.Contains(id))
                            continue;

                        foundMatches.Add(id);

                        yield return new Match(data.Data[w.Value].AsMemory().Slice(w.CharPosition, searchLength), lookBack, w);
                    }
                }

                searchLength--;
            }
        }

        private static int GetLookBack(int i, int length, int totalLength) {
            return MathHelper.Clamp(0, i + length - totalLength, i);
        }
    }
}