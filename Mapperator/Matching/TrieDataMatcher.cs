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
            mapDataPoints.Add(dataList);
            rhythmTrie.Add(ToRhythmString(dataList), index);
        }

        private ReadOnlyMemory<byte> ToRhythmString(IReadOnlyCollection<MapDataPoint> data) {
            byte[] rhythmString = new byte[data.Count];
            int i = 0;

            foreach (var mapDataPoint in data) {
                byte ho = ToRhythmToken(mapDataPoint);
                rhythmString[i] = ho;
                i++;
            }

            return rhythmString.AsMemory();
        }

        private byte ToRhythmToken(MapDataPoint mapDataPoint) {
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
            const int firstSearchLength = 8;

            var localPatternRhythmString = patternRhythmString ?? ToRhythmString(pattern);
            int searchLength = firstSearchLength;
            List<WordPosition<int>> result = new List<WordPosition<int>>();
            while (searchLength > 0 && result.Count == 0) {
                result = rhythmTrie.RetrieveSubstrings(localPatternRhythmString.Span.Slice(i - searchLength / 2, searchLength)).ToList();
                searchLength--;
            }

            if (result.Count == 0) {
                return mapDataPoints[0][0];
            }
            
            searchLength++;
            var best = result.First();
            var bestLength = 0;
            foreach (var wordPosition in result) {
                int length = GetMatchLength(wordPosition, localPatternRhythmString.Span[(i - searchLength)..]);
                if (lastId.HasValue && wordPosition.Value == lastId.Value.Value &&
                    wordPosition.CharPosition == lastId.Value.CharPosition + 1) {
                    pogs++;
                    best = wordPosition;
                    break;
                }
                if (length > bestLength) {
                    bestLength = length;
                    best = wordPosition;
                }
            }

            lastId = best;

            return GetMapDataPoint(best);
        }

        private MapDataPoint GetMapDataPoint(WordPosition<int> wordPosition, int offset = 0) {
            return mapDataPoints[wordPosition.Value][(int) wordPosition.CharPosition + offset];
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