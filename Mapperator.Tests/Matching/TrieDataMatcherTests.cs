using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gma.DataStructures.StringSearch;
using Mapperator.Matching.Matchers;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using NUnit.Framework;

namespace Mapperator.Tests.Matching;

public class TrieDataMatcherTests {
    private readonly List<MapDataPoint[]> mapDataPoints = new();
    private readonly UkkonenTrie<ushort, int> rhythmTrie = new(1);

    [OneTimeSetUp]
    public void Setup() {
        const string path = "Resources/input.osu";
        var data = new DataExtractor().ExtractBeatmapData(new BeatmapEditor(path).ReadFile()).ToArray();
        Add(data);
        Add(data);

        const string path2 = "Resources/input2.osu";
        var data2 = new DataExtractor().ExtractBeatmapData(new BeatmapEditor(path2).ReadFile());
        Add(data2);
    }

    private void Add(IEnumerable<MapDataPoint> data) {
        var dataList = data.ToArray();
        var index = mapDataPoints.Count;
        var rhythmString = TrieDataMatcher.ToRhythmString(dataList);
        mapDataPoints.Add(dataList);
        rhythmTrie.Add(rhythmString, index);
    }

    [Test]
    public void TestQuery() {
        foreach (var map in mapDataPoints) {
            var mapRhythmString = TrieDataMatcher.ToRhythmString(map);
            Debug.WriteLine(string.Join(',', mapRhythmString.ToArray()));

            for (var i = 0; i < map.Length; i++) {
                for (var searchLength = 1; searchLength < 10; searchLength++) {
                    var lookback = TrieDataMatcher.GetLookBack(i, searchLength, mapRhythmString.Length);
                    var query = mapRhythmString.Span.Slice(i - lookback, searchLength);
                    Debug.WriteLine(string.Join(',', query.ToArray()));

                    var result = rhythmTrie.RetrieveSubstrings(query).ToList();

                    Assert.IsTrue(result.Count > 0);
                    foreach (var wordPosition in result) {
                        //var rhythmString = dataRhythmStrings[wordPosition.Value];
                        //Console.WriteLine(string.Join('-', Enumerable.Range(0, searchLength).Select(o => rhythmString.Span[wordPosition.CharPosition + o])));
                        //Console.WriteLine(string.Join('-', query.ToArray()));
                        Assert.IsTrue(WordPositionInRange(wordPosition));
                        Assert.IsTrue(WordPositionInRange(wordPosition, searchLength - 1));
                        Assert.AreEqual(searchLength, GetMatchLength(wordPosition, query));
                    }

                    var (min, max) = TrieDataMatcher.ToDistanceRange(query, 10);
                    var rangeResult = rhythmTrie.RetrieveSubstringsRange(min, max).ToList();

                    Assert.IsTrue(rangeResult.Count > 0);
                    foreach (var wordPosition in rangeResult) {
                        //var rhythmString = dataRhythmStrings[wordPosition.Value];
                        //Console.WriteLine(string.Join('-', Enumerable.Range(0, searchLength).Select(o => rhythmString.Span[wordPosition.CharPosition + o])));
                        //Console.WriteLine(string.Join('-', query.ToArray()));
                        Assert.IsTrue(WordPositionInRange(wordPosition));
                        Assert.IsTrue(WordPositionInRange(wordPosition, searchLength - 1));
                        Assert.AreEqual(searchLength, GetMatchLengthRange(wordPosition, min.Span, max.Span));
                    }
                }
            }
        }
    }

    private MapDataPoint GetMapDataPoint(WordPosition<int> wordPosition, int offset = 0) {
        return mapDataPoints[wordPosition.Value][wordPosition.CharPosition + offset];
    }

    private bool WordPositionInRange(WordPosition<int> wordPosition, int offset = 0) {
        return wordPosition.Value < mapDataPoints.Count && wordPosition.Value >= 0 &&
               wordPosition.CharPosition + offset < mapDataPoints[wordPosition.Value].Length &&
               wordPosition.CharPosition + offset >= 0;
    }

    private int GetMatchLength(WordPosition<int> wordPosition, ReadOnlySpan<ushort> pattern) {
        var length = 0;
        while (wordPosition.CharPosition + length < mapDataPoints[wordPosition.Value].Length &&
               length < pattern.Length &&
               TrieDataMatcher.ToRhythmToken(GetMapDataPoint(wordPosition, length)) ==
               pattern[length]) {
            length++;
        }

        return length;
    }

    private int GetMatchLengthRange(WordPosition<int> wordPosition, ReadOnlySpan<ushort> min, ReadOnlySpan<ushort> max) {
        var length = 0;
        while (wordPosition.CharPosition + length < mapDataPoints[wordPosition.Value].Length &&
               length < min.Length &&
               TrieDataMatcher.ToRhythmToken(GetMapDataPoint(wordPosition, length)) >= min[length] &&
               TrieDataMatcher.ToRhythmToken(GetMapDataPoint(wordPosition, length)) <= max[length]) {
            length++;
        }

        return length;
    }
}