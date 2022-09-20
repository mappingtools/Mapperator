using Gma.DataStructures.StringSearch;
using Mapperator.Matching.DataStructures;
using Mapperator.Matching.Judges;
using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching.Matchers {
    public class TrieDataMatcher : IDataMatcher {
        private const int FirstSearchLength = 32;
        private static readonly int[] DistanceRanges = { 0, 3, 9 };  // Best values found by trial-and-error
        private const double PogBonus = 50;
        private const int MaxLookBack = 8;
        private const int MaxSearch = 100000;

        private readonly RhythmDistanceTrieStructure data;
        private readonly ReadOnlyMemory<MapDataPoint> pattern;
        private readonly IJudge judge;
        private readonly (ReadOnlyMemory<ushort>, ReadOnlyMemory<ushort>)[] patternRhythmString;

        private WordPosition<int>? lastId;
        private int? lastLength;
        private int? lastLookBack;
        private int pogs;
        private double totalScore;
        private double totalMatchingCost;
        private double totalRelationScore;
        private int totalSearched;

        public TrieDataMatcher(RhythmDistanceTrieStructure data, ReadOnlyMemory<MapDataPoint> pattern) : this(data, pattern, new SuperJudge()) { }

        public TrieDataMatcher(RhythmDistanceTrieStructure data, ReadOnlyMemory<MapDataPoint> pattern, IJudge judge) {
            this.data = data;
            this.pattern = pattern;
            this.judge = judge;
            patternRhythmString = RhythmDistanceTrieStructure.ToDistanceRanges(RhythmDistanceTrieStructure.ToRhythmString(pattern.Span).Span, DistanceRanges);
        }

        public IEnumerable<MapDataPoint> FindSimilarData(Func<MapDataPoint, bool> isValidFunc) {
            Console.WriteLine("Searching for matches");
            lastId = null;
            pogs = 0;
            totalScore = 0;
            totalMatchingCost = 0;
            totalRelationScore = 0;
            for (var i = 0; i < pattern.Length; i++) {
                var match = FindBestMatch(i, isValidFunc);
                yield return match;
            }
            Console.WriteLine($"Pograte = {(float)pogs / pattern.Length}");
            Console.WriteLine($"Score = {totalScore / pattern.Length}");
            Console.WriteLine($"Avg matching cost = {totalMatchingCost / pattern.Length}");
            Console.WriteLine($"Avg relation score = {totalRelationScore / pattern.Length}");
            Console.WriteLine($"Avg searched = {totalSearched / pattern.Length}");
        }

        public MapDataPoint FindBestMatch(int i, Func<MapDataPoint, bool> isValidFunc) {
            var searchLength = Math.Min(FirstSearchLength, pattern.Length);
            var numSearched = 0;

            var bestScore = double.NegativeInfinity;
            var best = new WordPosition<int>(0, 0);
            var bestLength = 0;
            var bestLookBack = 0;
            //var bestWidth = 0;

            // First try the pog option
            if (lastId.HasValue && lastLength.HasValue && lastLookBack.HasValue && lastLength - lastLookBack > 1) {
                var pogLength = lastLength.Value;
                var lookBack = lastLookBack.Value + 1;
                var pogPos = new WordPosition<int>(lastId.Value.CharPosition + 1, lastId.Value.Value);

                if (!IsValidSeries(pogPos, pogLength - lookBack, isValidFunc)) {
                    goto PogTried;
                }

                // Rate the quality of the match
                bestScore = RateMatchQuality(pogPos, i, pogLength, lookBack) + PogBonus;
                best = pogPos;
                bestLength = pogLength;
                bestLookBack = lookBack;
            }
            PogTried:

            while (searchLength > 0 && bestScore < 0.5 * BestPossibleScore(i, searchLength, pattern.Length)) {
                var lookBack = GetLookBack(i, searchLength, pattern.Length);

                for (var j = 0; j < patternRhythmString.Length; j++) {
                    var min = patternRhythmString[j].Item1.Slice(i - lookBack, searchLength);
                    var max = patternRhythmString[j].Item2.Slice(i - lookBack, searchLength);
                    var foundAnything = false;

                    var result = data.Trie.RetrieveSubstringsRange(min, max);

                    // Find the best match
                    foreach (var wordPosition in result) {
                        if (numSearched++ >= MaxSearch) {
                            break;
                        }

                        // Get the position of the middle data point
                        var middlePos = new WordPosition<int>(wordPosition.CharPosition + lookBack, wordPosition.Value);

                        if (!IsValidSeries(middlePos, searchLength - lookBack, isValidFunc)) {
                            continue;
                        }

                        foundAnything = true;

                        // Rate the quality of the match
                        var score = RateMatchQuality(middlePos, i, searchLength, lookBack);

                        if (!(score > bestScore)) continue;

                        bestScore = score;
                        best = middlePos;
                        bestLength = searchLength;
                        bestLookBack = lookBack;
                        //bestWidth = j;
                    }

                    if (foundAnything)
                        break;
                }

                searchLength--;

                if (numSearched >= MaxSearch) {
                    break;
                }
            }

            totalScore += double.IsNegativeInfinity(bestScore) ? 0 : bestScore;
            var matchingCost = judge.MatchingCost(pattern.Span[i], data.GetMapDataPoint(best));
            var relationScore = judge.RelationScore(pattern.Span, data.Data[best.Value].AsSpan(), i, best.CharPosition, 50);
            totalMatchingCost += matchingCost;
            totalRelationScore += relationScore;
            totalSearched += numSearched;
            if (lastId.HasValue && best.Value == lastId.Value.Value &&
                best.CharPosition == lastId.Value.CharPosition + 1) {
                pogs++;
            }

            lastId = best;
            lastLength = bestLength;
            lastLookBack = bestLookBack;
            Console.WriteLine($"match {i}, id = {lastId}, num searched = {numSearched}, length = {bestLength}, score = {bestScore}, matching cost = {matchingCost}, relation = {relationScore}");
            //var bestmin = localPatternRhythmString[bestWidth].Item1.Slice(i - bestLookBack, bestLength);
            //var bestmax = localPatternRhythmString[bestWidth].Item2.Slice(i - bestLookBack, bestLength);
            //Console.WriteLine($"match code = {string.Join(',', Enumerable.Range(-bestLookBack, bestLength).Select(o => ToRhythmToken(GetMapDataPoint(best, o))))}; min = {string.Join(',', bestmin.ToArray())}; max = {string.Join(',', bestmax.ToArray())}");

            return data.GetMapDataPoint(best);
        }

        public static int GetLookBack(int i, int length, int totalLength) {
            return MathHelper.Clamp(Math.Min(length / 2, MaxLookBack), i + length - totalLength, i);
        }

        private double BestPossibleScore(int i, int length, int totalLength) {
            return judge.BestPossibleScore(length, GetLookBack(i, length, totalLength));
        }

        private double RateMatchQuality(WordPosition<int> pos, int index, int length, int lookBack) {
            return judge.Judge(data.Data[pos.Value].AsSpan().Slice(pos.CharPosition - lookBack, length),
                pattern.Span.Slice(index - lookBack, length), lookBack);
        }

        private bool IsValidSeries(WordPosition<int> wordPosition, int count, Func<MapDataPoint, bool> isValidFunc) {
            double cumulativeAngle = 0;
            var pos = Vector2.Zero;
            double beatsSince = 0;
            for (var i = 0; i < count; i++) {
                var dataPoint = data.GetMapDataPoint(wordPosition, i);

                cumulativeAngle += dataPoint.Angle;
                var dir = Vector2.Rotate(Vector2.UnitX, cumulativeAngle);
                pos += dataPoint.Spacing * dir;
                beatsSince += dataPoint.BeatsSince;

                // Test a new datapoint with the cumulated angle and distance and gap
                dataPoint.BeatsSince = beatsSince;
                dataPoint.Spacing = pos.Length;
                dataPoint.Angle = pos.Theta;

                if (!isValidFunc(dataPoint)) {
                    return false;
                }
            }

            return true;
        }
    }
}