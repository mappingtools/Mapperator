using System;
using System.Diagnostics;
using System.Linq;
using Mapperator.Matching.DataStructures;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using NUnit.Framework;
using TrieNet;

namespace Mapperator.Tests.Matching;

public class TrieDataMatcherTests {
    private RhythmDistanceTrieStructure data;

    [OneTimeSetUp]
    public void Setup() {
        data = new RhythmDistanceTrieStructure();

        const string path = "Resources/input.osu";
        var data1 = new DataExtractor().ExtractBeatmapData(new BeatmapEditor(path).ReadFile()).ToArray();
        data.Add(data1);
        data.Add(data1);

        const string path2 = "Resources/input2.osu";
        var data2 = new DataExtractor().ExtractBeatmapData(new BeatmapEditor(path2).ReadFile());
        data.Add(data2.ToArray());
    }

    [Test]
    public void TestQuery() {
        foreach (var map in data.Data) {
            var mapRhythmString = RhythmDistanceTrieStructure.ToRhythmString(map);
            Debug.WriteLine(string.Join(',', mapRhythmString.ToArray()));

            for (var i = 0; i < map.Length; i++) {
                var maxLength = Math.Min(10, map.Length - i);
                for (var searchLength = 1; searchLength < maxLength; searchLength++) {
                    var query = mapRhythmString.Span.Slice(i, searchLength);
                    Debug.WriteLine(string.Join(',', query.ToArray()));

                    var result = data.Trie.RetrieveSubstrings(query).ToList();

                    Assert.IsTrue(result.Count > 0);
                    foreach (var wordPosition in result) {
                        //var rhythmString = dataRhythmStrings[wordPosition.Value];
                        //Console.WriteLine(string.Join('-', Enumerable.Range(0, searchLength).Select(o => rhythmString.Span[wordPosition.CharPosition + o])));
                        //Console.WriteLine(string.Join('-', query.ToArray()));
                        Assert.IsTrue(data.WordPositionInRange(wordPosition));
                        Assert.IsTrue(data.WordPositionInRange(wordPosition, searchLength - 1));
                        Assert.AreEqual(searchLength, GetMatchLength(wordPosition, query));
                    }

                    var (min, max) = RhythmDistanceTrieStructure.ToDistanceRange(query, 10);
                    var rangeResult = data.Trie.RetrieveSubstringsRange(min, max).ToList();

                    Assert.IsTrue(rangeResult.Count > 0);
                    foreach (var wordPosition in rangeResult) {
                        //var rhythmString = dataRhythmStrings[wordPosition.Value];
                        //Console.WriteLine(string.Join('-', Enumerable.Range(0, searchLength).Select(o => rhythmString.Span[wordPosition.CharPosition + o])));
                        //Console.WriteLine(string.Join('-', query.ToArray()));
                        Assert.IsTrue(data.WordPositionInRange(wordPosition));
                        Assert.IsTrue(data.WordPositionInRange(wordPosition, searchLength - 1));
                        Assert.AreEqual(searchLength, GetMatchLengthRange(wordPosition, min.Span, max.Span));
                    }
                }
            }
        }
    }

    private int GetMatchLength(WordPosition<int> wordPosition, ReadOnlySpan<RhythmToken> pattern) {
        var length = 0;
        while (wordPosition.CharPosition + length < data.Data[wordPosition.Value].Length &&
               length < pattern.Length &&
               RhythmDistanceTrieStructure.ToRhythmToken(data.GetMapDataPoint(wordPosition, length)) ==
               pattern[length]) {
            length++;
        }

        return length;
    }

    private int GetMatchLengthRange(WordPosition<int> wordPosition, ReadOnlySpan<RhythmToken> min, ReadOnlySpan<RhythmToken> max) {
        var length = 0;
        while (wordPosition.CharPosition + length < data.Data[wordPosition.Value].Length &&
               length < min.Length &&
               RhythmDistanceTrieStructure.ToRhythmToken(data.GetMapDataPoint(wordPosition, length)) >= min[length] &&
               RhythmDistanceTrieStructure.ToRhythmToken(data.GetMapDataPoint(wordPosition, length)) <= max[length]) {
            length++;
        }

        return length;
    }
}