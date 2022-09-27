using System;
using System.Runtime.InteropServices;
using Mapperator.Matching.DataStructures;
using NUnit.Framework;

namespace Mapperator.Tests.Matching.DataStructures;

public class RhythmDistanceTrieTests {
    private RhythmDistanceTrie trie;

    [OneTimeSetUp]
    public void Setup() {
        trie = new RhythmDistanceTrie();
    }

    [Test]
    [Explicit]
    public void TestRhythmTokenSize() {
        Console.WriteLine(Marshal.SizeOf<RhythmToken>());
    }
}