using Mapperator.Matching.DataStructures;
using Mapperator.Matching.Judges;
using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;
using TrieNet;

namespace Mapperator.Matching.Matchers {
    public class TrieDataMatcher : IDataMatcher {
        private const double PogBonus = 80;
        private const int MaxSearch = 100000;

        private readonly RhythmDistanceTrieStructure data;
        private readonly ReadOnlyMemory<MapDataPoint> pattern;
        private readonly IJudge judge;
        private readonly ReadOnlyMemory<RhythmToken> patternRhythmString;

        private WordPosition<int>? lastId;
        private double? lastMult;
        private int? lastLength;
        private int? lastLookBack;
        private int failedMatches;
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
            patternRhythmString = RhythmDistanceTrieStructure.ToRhythmString(pattern.Span);
        }

        public IEnumerable<(MapDataPoint, double)> FindSimilarData(Func<MapDataPoint, bool> isValidFunc) {
            Console.WriteLine("Searching for matches");
            lastId = null;
            failedMatches = 0;
            pogs = 0;
            totalScore = 0;
            totalMatchingCost = 0;
            totalRelationScore = 0;
            for (var i = 0; i < pattern.Length; i++) {
                var match = FindBestMatch(i, isValidFunc);
                yield return match;
            }
            Console.WriteLine($"Failed matches = {failedMatches}");
            Console.WriteLine($"Pograte = {(float)pogs / pattern.Length}");
            Console.WriteLine($"Score = {totalScore / pattern.Length}");
            Console.WriteLine($"Avg matching cost = {totalMatchingCost / pattern.Length}");
            Console.WriteLine($"Avg relation score = {totalRelationScore / pattern.Length}");
            Console.WriteLine($"Avg searched = {totalSearched / pattern.Length}");
        }

        public (MapDataPoint, double) FindBestMatch(int i, Func<MapDataPoint, bool> isValidFunc) {
            var numSearched = 0;

            var bestScore = double.NegativeInfinity;
            var best = new WordPosition<int>(0, 0);
            var bestLength = 0;
            var bestLookBack = 0;
            var bestMult = 1d;
            //var bestWidth = 0;

            // First try the pog option
            if (lastId.HasValue && lastLength.HasValue && lastLookBack.HasValue && lastMult.HasValue && lastLength - lastLookBack > 1) {
                var pogLength = lastLength.Value;
                var lookBack = lastLookBack.Value + 1;
                var pogPos = new WordPosition<int>(lastId.Value.CharPosition + 1, lastId.Value.Value);

                // Rate the quality of the match
                bestScore = RateMatchQuality(pogPos, i, pogLength, lookBack, lastMult.Value) + PogBonus;
                best = pogPos;
                bestLength = pogLength;
                bestLookBack = lookBack;
                bestMult = lastMult.Value;
            }

            var searchLength = pattern.Length - i;
            var minLength = Math.Max(1, judge.MinLengthForScore(bestScore));
            var boxedLength = new RhythmDistanceTrie.MinLengthProvider(minLength);
            var query = patternRhythmString.Slice(i, searchLength);
            var result = data.Trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, boxedLength);

            // Find the best match
            foreach (var (wordPosition, length, minMult, maxMult) in result) {
                if (numSearched++ >= MaxSearch) {
                    break;
                }

                // Get the multiplier in the middle
                var mult = Math.Sqrt(minMult * maxMult);

                var validLength = IsValidSeriesLength(wordPosition, length, isValidFunc, mult);
                if (validLength < 1) continue;

                // Rate the quality of the match
                var score = RateMatchQuality(wordPosition, i, validLength, 0, mult);

                if (!(score > bestScore)) continue;

                bestScore = score;
                best = wordPosition;
                bestLength = validLength;
                bestLookBack = 0;
                bestMult = mult;

                boxedLength.MinLength = Math.Max(boxedLength.MinLength, judge.MinLengthForScore(bestScore));
            }

            totalScore += double.IsNegativeInfinity(bestScore) ? 0 : bestScore;
            var matchingCost = judge.MatchingCost(pattern.Span[i], data.GetMapDataPoint(best), bestMult);
            var relationScore = judge.RelationScore(pattern.Span, data.Data[best.Value].AsSpan(), i, best.CharPosition, 50, bestMult);
            totalMatchingCost += matchingCost;
            totalRelationScore += relationScore;
            totalSearched += numSearched;
            if (lastId.HasValue && best.Value == lastId.Value.Value &&
                best.CharPosition == lastId.Value.CharPosition + 1) {
                pogs++;
            }

            if (double.IsNegativeInfinity(bestScore))
                failedMatches++;

            lastId = best;
            lastLength = bestLength;
            lastLookBack = bestLookBack;
            lastMult = bestMult;
            Console.WriteLine($"match {i}, id = {lastId}, num searched = {numSearched}, length = {bestLength}, mult = {bestMult}, score = {bestScore}, matching cost = {matchingCost}, relation = {relationScore}");
            //var bestmin = localPatternRhythmString[bestWidth].Item1.Slice(i - bestLookBack, bestLength);
            //var bestmax = localPatternRhythmString[bestWidth].Item2.Slice(i - bestLookBack, bestLength);
            //Console.WriteLine($"match code = {string.Join(',', Enumerable.Range(-bestLookBack, bestLength).Select(o => ToRhythmToken(GetMapDataPoint(best, o))))}; min = {string.Join(',', bestmin.ToArray())}; max = {string.Join(',', bestmax.ToArray())}");

            return (data.GetMapDataPoint(best), bestMult);
        }

        private double RateMatchQuality(WordPosition<int> pos, int index, int length, int lookBack, double mult) {
            return judge.Judge(data.Data[pos.Value].AsSpan().Slice(pos.CharPosition - lookBack, length),
                pattern.Span.Slice(index - lookBack, length), lookBack, mult);
        }

        private int IsValidSeriesLength(WordPosition<int> wordPosition, int count, Func<MapDataPoint, bool> isValidFunc, double mult) {
            double cumulativeAngle = 0;
            var pos = Vector2.Zero;
            double beatsSince = 0;
            var length = 0;
            for (var i = 0; i < count; i++) {
                var dataPoint = data.GetMapDataPoint(wordPosition, i);

                cumulativeAngle += dataPoint.Angle;
                var dir = Vector2.Rotate(Vector2.UnitX, cumulativeAngle);
                pos += dataPoint.Spacing * mult * dir;
                beatsSince += dataPoint.BeatsSince;

                // Test a new datapoint with the cumulated angle and distance and gap
                dataPoint.BeatsSince = beatsSince;
                dataPoint.Spacing = pos.Length;
                dataPoint.Angle = pos.Theta;
                if (double.IsNaN(dataPoint.Angle)) {
                    dataPoint.Angle = 0;
                }

                if (!isValidFunc(dataPoint)) {
                    return length;
                }

                length++;
            }

            return length;
        }
    }
}