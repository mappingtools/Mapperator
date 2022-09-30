using System;
using System.Runtime.InteropServices;
using Mapperator.Matching.DataStructures;
using NUnit.Framework;

namespace Mapperator.Tests.Matching.DataStructures;

public class RhythmDistanceTrieTests {
    private RhythmDistanceTrie trie;

    [OneTimeSetUp]
    public void Setup() {
        trie = new RhythmDistanceTrie(1);
    }

    [Test]
    [Explicit]
    public void TestRhythmTokenSize() {
        Console.WriteLine(Marshal.SizeOf<RhythmToken>());
    }
}