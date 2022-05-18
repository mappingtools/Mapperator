using Mapperator.Matching.DataStructures;
using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching.Matchers {
    public class TrieDataMatcher2 : IDataMatcher2 {
        private const int FirstSearchLength = 32;
        private static readonly double[] DistanceRanges = { 0, 3, 9 };  // Best values found by trial-and-error
        private const int MaxLookBack = 8;

        private readonly RhythmDistanceTrieStructure data;
        private readonly ReadOnlyMemory<ushort> patternRhythmString;

        private readonly HashSet<int> foundMatches = new();

        public TrieDataMatcher2(RhythmDistanceTrieStructure data, ReadOnlySpan<MapDataPoint> pattern) {
            this.data = data;
            patternRhythmString = data.ToRhythmString(pattern);
        }

        public IEnumerable<Match> FindMatches(int i) {
            foundMatches.Clear();
            var searchLength = Math.Min(FirstSearchLength, patternRhythmString.Length);

            while (searchLength > 0) {
                var lookBack = GetLookBack(i, searchLength, patternRhythmString.Length);
                var query = patternRhythmString.Span.Slice(i - lookBack, searchLength);

                foreach (var width in DistanceRanges) {
                    var (min, max) = data.ToDistanceRange(query, d => RangeFunction(d, (int)width));

                    var result = data.Trie.RetrieveSubstringsRange(min, max);

                    // Yield all new matches
                    foreach (var w in result) {
                        var id = w.CharPosition + lookBack;

                        // Skip if this ID was already found
                        if (foundMatches.Contains(id))
                            continue;

                        foundMatches.Add(id);

                        yield return new Match(data.Data[w.Value].AsMemory().Slice(w.CharPosition, searchLength),
                            lookBack, w.CharPosition + lookBack);
                    }
                }

                searchLength--;
            }
        }

        private (int, int) RangeFunction(int i, int width) {
            return (i - width, i + width);
        }

        private static int GetLookBack(int i, int length, int totalLength) {
            return MathHelper.Clamp(Math.Min(length / 2, MaxLookBack), i + length - totalLength, i);
        }
    }
}