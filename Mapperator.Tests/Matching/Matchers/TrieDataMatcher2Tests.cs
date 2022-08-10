using System;
using System.Linq;
using Mapperator.Matching.DataStructures;
using Mapperator.Matching.Matchers;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using NUnit.Framework;

namespace Mapperator.Tests.Matching.Matchers;

public class TrieDataMatcher2Tests {
    private readonly RhythmDistanceTrieStructure data = new();
    private TrieDataMatcher2 matcher;

    [OneTimeSetUp]
    public void Setup() {
        const string path = "Resources/input.osu";
        var dataPoints = new DataExtractor().ExtractBeatmapData(new BeatmapEditor(path).ReadFile()).ToArray();
        data.Add(dataPoints);
    }

    [Test]
    public void TestQuery() {
        matcher = new TrieDataMatcher2(data, data.Data[0].AsSpan());

        var result = matcher.FindMatches(0);

        foreach (var match in result) {
            Console.WriteLine(match.Seq.Length);
            Console.WriteLine(match.WantedIndex);
            Console.WriteLine(string.Join('-', data.ToRhythmString(match.Seq.Span).ToArray()));
            //Assert.IsTrue(WordPositionInRange(wordPosition));
            //Assert.IsTrue(WordPositionInRange(wordPosition, searchLength));
            //Assert.AreEqual(searchLength, GetMatchLength(wordPosition, query));
        }
    }
}