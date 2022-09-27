using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Gma.DataStructures.StringSearch;
using Mapperator.Matching.DataStructures;
using Mapperator.Matching.Matchers;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using NUnit.Framework;

namespace Mapperator.Tests.Matching.DataStructures;

public class RhythmDistanceTrieTests {
    private RhythmDistanceTrie trie;

    [OneTimeSetUp]
    public void Setup() {
        trie = new RhythmDistanceTrie();

        const string path = "Resources/input.osu";
        var data1 = new DataExtractor().ExtractBeatmapData(new BeatmapEditor(path).ReadFile()).ToArray();
        trie.Add(ToRhythmString(data1), 0);
    }

    private static ReadOnlyMemory<RhythmToken> ToRhythmString(ReadOnlySpan<MapDataPoint> data) {
        var rhythmString = new RhythmToken[data.Length];
        var i = 0;

        foreach (var mapDataPoint in data) {
            rhythmString[i] = new RhythmToken(mapDataPoint);
            i++;
        }

        return rhythmString.AsMemory();
    }

    [Test]
    [Explicit]
    public void TestRhythmTokenSize() {
        Console.WriteLine(Marshal.SizeOf<RhythmToken>());
    }
}