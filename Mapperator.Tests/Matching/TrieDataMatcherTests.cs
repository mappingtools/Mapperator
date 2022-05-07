using System;
using System.Collections.Generic;
using System.Linq;
using Gma.DataStructures.StringSearch;
using Mapperator.ConsoleApp;
using Mapperator.Matching;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using NUnit.Framework;

namespace Mapperator.Tests.Matching;

public class TrieDataMatcherTests {
    private readonly List<List<MapDataPoint>> mapDataPoints = new();
    private readonly UkkonenTrie<byte, int> rhythmTrie = new(1);

    [OneTimeSetUp]
    public void Setup() {
        const string path = "Resources/input.osu";
        var data = new DataExtractor().ExtractBeatmapData(new BeatmapEditor(path).ReadFile()).ToList();

        var dataList = data.ToList();
        var index = mapDataPoints.Count;
        var rhythmString = TrieDataMatcher.ToRhythmString(dataList);
        mapDataPoints.Add(dataList);
        rhythmTrie.Add(rhythmString, index);
    }

    [TestCase(new byte[] { 4 })]
    [TestCase(new byte[] { 4, 4, 4, 4 })]
    [TestCase(new byte[] { 6, 5, 5, 23, 6, 5, 5, 23, 6, 5, 5, 23, 6, 5, 5, 24 })]
    public void TestQuery(byte[] queryArray) {
        var query = queryArray.AsSpan();
        var searchLength = query.Length;

        var result = rhythmTrie
            .RetrieveSubstrings(query);

        foreach (var wordPosition in result) {
            //var rhythmString = dataRhythmStrings[wordPosition.Value];
            //Console.WriteLine(string.Join('-', Enumerable.Range(0, searchLength).Select(o => rhythmString.Span[wordPosition.CharPosition + o])));
            //Console.WriteLine(string.Join('-', query.ToArray()));
            Assert.IsTrue(WordPositionInRange(wordPosition));
            Assert.IsTrue(WordPositionInRange(wordPosition, searchLength));
            Assert.AreEqual(searchLength, GetMatchLength(wordPosition, query));
        }
    }

    private MapDataPoint GetMapDataPoint(WordPosition<int> wordPosition, int offset = 0) {
        return mapDataPoints[wordPosition.Value][wordPosition.CharPosition + offset];
    }

    private bool WordPositionInRange(WordPosition<int> wordPosition, int offset = 0) {
        return wordPosition.Value < mapDataPoints.Count && wordPosition.Value >= 0 &&
               wordPosition.CharPosition + offset < mapDataPoints[wordPosition.Value].Count &&
               wordPosition.CharPosition + offset >= 0;
    }

    private int GetMatchLength(WordPosition<int> wordPosition, ReadOnlySpan<byte> pattern) {
        var length = 0;
        while (wordPosition.CharPosition + length < mapDataPoints[wordPosition.Value].Count &&
               length < pattern.Length &&
               TrieDataMatcher.ToRhythmToken(GetMapDataPoint(wordPosition, length)) ==
               pattern[length]) {
            length++;
        }

        return length;
    }
}