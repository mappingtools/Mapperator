using System;
using System.Collections.Generic;
using System.Linq;
using Gma.DataStructures.StringSearch;
using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching {
    public class TrieDataMatcher : IDataMatcher {
        private readonly List<List<MapDataPoint>> mapDataPoints = new();
        private readonly UkkonenTrie<byte, int> rhythmTrie = new(1);

        private WordPosition<int>? lastId;
        private int pogs;
        private ReadOnlyMemory<byte>? patternRhythmString;

        public void AddData(IEnumerable<MapDataPoint> data) {
            var dataList = data.ToList();
            var index = mapDataPoints.Count;
            var rhythmString = ToRhythmString(dataList);
            mapDataPoints.Add(dataList);
            rhythmTrie.Add(rhythmString, index);
        }

        public static ReadOnlyMemory<byte> ToRhythmString(IReadOnlyCollection<MapDataPoint> data) {
            byte[] rhythmString = new byte[data.Count];
            int i = 0;

            foreach (var mapDataPoint in data) {
                byte ho = ToRhythmToken(mapDataPoint);
                rhythmString[i] = ho;
                i++;
            }

            return rhythmString.AsMemory();
        }

        public static byte ToRhythmToken(MapDataPoint mapDataPoint) {
            const int gapResolution = 6;
            const int gapRange = 9;
            byte gap = (byte) MathHelper.Clamp((int) Math.Log2(mapDataPoint.BeatsSince) + gapResolution, 0, gapRange - 1);
            return mapDataPoint.DataType switch {
                DataType.Hit => gap,
                DataType.Spin => (byte)(gapRange + gap),
                DataType.Release => mapDataPoint.Repeats switch {
                    0 => (byte)(gapRange * 2 + gap),
                    1 => (byte)(gapRange * 3 + gap),
                    _ => (byte)(gapRange * 4 + gap)
                },
                _ => gap
            };
        }

        public IEnumerable<MapDataPoint> FindSimilarData(IReadOnlyList<MapDataPoint> pattern, Func<MapDataPoint, bool> isValidFunc = null) {
            Console.WriteLine("Searching for matches");
            // We want to replace the previous parts of the pattern with the matches we found so the next matches have a better chance
            // of continuing the previous pattern
            patternRhythmString = ToRhythmString(pattern);
            var newPattern = pattern.ToArray();
            lastId = null;
            pogs = 0;
            for (int i = 0; i < pattern.Count; i++) {
                var match = FindBestMatch(newPattern, i, isValidFunc);
                newPattern[i] = match;
                yield return match;
            }

            patternRhythmString = null;
            Console.WriteLine($"Pograte = {(float)pogs / pattern.Count}");
        }

        public MapDataPoint FindBestMatch(IReadOnlyList<MapDataPoint> pattern, int i, Func<MapDataPoint, bool> isValidFunc = null) {
            const int firstSearchLength = 32;

            var localPatternRhythmString = patternRhythmString ?? ToRhythmString(pattern);
            var searchLength = Math.Min(firstSearchLength, localPatternRhythmString.Length);
            var best = new WordPosition<int>(0, 0);
            var bestLength = -1;
            while (searchLength > 0 && bestLength == -1) {
                var lookBack = MathHelper.Clamp(searchLength / 2, i + searchLength - localPatternRhythmString.Length, i);
                var query = localPatternRhythmString.Span.Slice(i - lookBack, searchLength);

                var result = rhythmTrie
                    .RetrieveSubstrings(query);

                // Find the best match
                foreach (var wordPosition in result) {
                    // Get the position of the middle data point
                    var middlePos = new WordPosition<int>(wordPosition.CharPosition + lookBack, wordPosition.Value);

                    if (isValidFunc is not null && !IsValidSeries(middlePos, searchLength - lookBack, isValidFunc)) {
                        continue;
                    }

                    if (lastId.HasValue && wordPosition.Value == lastId.Value.Value &&
                        middlePos.CharPosition == lastId.Value.CharPosition + 1) {
                        bestLength = searchLength;
                        best = middlePos;
                        pogs++;
                        break;
                    }

                    if (searchLength > bestLength) {
                        bestLength = searchLength;
                        best = middlePos;
                    }
                }
                searchLength--;
            }

            lastId = best;
            Console.WriteLine($"match {i}, id = {lastId}, length = {bestLength}");

            return GetMapDataPoint(best);
        }

        private bool IsValidSeries(WordPosition<int> wordPosition, int count, Func<MapDataPoint, bool> isValidFunc) {
            double cumulativeAngle = 0;
            Vector2 pos = Vector2.Zero;
            double beatsSince = 0;
            for (int i = 0; i < count; i++) {
                var dataPoint = GetMapDataPoint(wordPosition, i);

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

        private MapDataPoint GetMapDataPoint(WordPosition<int> wordPosition, int offset = 0) {
            return mapDataPoints[wordPosition.Value][(int) wordPosition.CharPosition + offset];
        }

        private bool WordPositionInRange(WordPosition<int> wordPosition, int offset = 0) {
            return wordPosition.Value < mapDataPoints.Count && wordPosition.Value >= 0 &&
                wordPosition.CharPosition + offset < mapDataPoints[wordPosition.Value].Count &&
                wordPosition.CharPosition + offset >= 0;
        }

        private int GetMatchLength(WordPosition<int> wordPosition, ReadOnlySpan<byte> pattern) {
            int length = 0;
            while (wordPosition.CharPosition + length < mapDataPoints[wordPosition.Value].Count &&
                   length < pattern.Length &&
                ToRhythmToken(GetMapDataPoint(wordPosition, length)) ==
                   pattern[length]) {
                length++;
            }

            return length;
        }
    }
}