using System;
using System.Linq;
using Mapperator.Matching.DataStructures;
using Mapperator.Matching.Matchers;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using NUnit.Framework;

namespace Mapperator.Tests.Matching.Matchers;

public class TrieDataMatcherTests {
    private readonly RhythmDistanceTrieStructure data = new();
    private TrieDataMatcher matcher;

    [OneTimeSetUp]
    public void Setup() {
        const string path = "Resources/input.osu";
        var dataPoints = new DataExtractor().ExtractBeatmapData(new BeatmapEditor(path).ReadFile()).ToArray();
        data.Add(dataPoints);
    }

    [Test]
    public void TestQuery() {
        matcher = new TrieDataMatcher(data, data.Data[0].AsSpan());

        var result = matcher.FindMatches(0);

        foreach (var match in result) {
            Console.WriteLine(match.Sequence.Length);
            Console.WriteLine(string.Join('-', RhythmDistanceTrieStructure.ToRhythmString(match.Sequence.Span).ToArray()));
            //Assert.That(WordPositionInRange(wordPosition));
            //Assert.That(WordPositionInRange(wordPosition, searchLength));
            //Assert.AreEqual(searchLength, GetMatchLength(wordPosition, query));
        }
    }
}