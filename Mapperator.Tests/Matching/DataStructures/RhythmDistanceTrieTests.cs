using System;
using System.Linq;
using System.Runtime.InteropServices;
using Mapperator.Matching.DataStructures;
using NUnit.Framework;

namespace Mapperator.Tests.Matching.DataStructures;

public class RhythmDistanceTrieTests {
    private RhythmDistanceTrie trie;

    [OneTimeSetUp]
    public void Setup() {
        trie = new RhythmDistanceTrie(1);
        trie.Add(new[] {
            new RhythmToken(0, 0, 0),
            new RhythmToken(0, 4, 40),
            new RhythmToken(0, 4, 80),
            new RhythmToken(1, 8, 80),
            new RhythmToken(0, 2, 20),
            new RhythmToken(0, 4, 80)
        }.AsMemory(), 0);
    }

    [Test]
    public void TestRetrieveSubstringsDynamicLengthAndDistanceRange() {
        var query = new[] {
            new RhythmToken(0, 0, 0),
            new RhythmToken(0, 4, 40),
            new RhythmToken(0, 4, 80),
            new RhythmToken(1, 8, 80),
            new RhythmToken(0, 2, 20),
            new RhythmToken(0, 4, 80)
        }.AsMemory();

        var result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, 1).ToArray();

        Assert.AreEqual(1, result.Length);
        var (pos, length, minMult, maxMult) = result[0];
        Console.WriteLine(result[0]);
        Assert.AreEqual(0, pos.Value);
        Assert.AreEqual(0, pos.CharPosition);
        Assert.AreEqual(6, length);
        Assert.IsTrue(minMult < 1 && 1 < maxMult);
    }

    [Test]
    public void TestDynamicLengthRetrieveSubstringsDynamicLengthAndDistanceRange() {
        var query = new[] {
            new RhythmToken(0, 0, 0),
            new RhythmToken(0, 4, 40),
            new RhythmToken(2, 4, 80),
            new RhythmToken(1, 8, 80),
            new RhythmToken(0, 2, 20),
            new RhythmToken(0, 4, 80)
        }.AsMemory();

        var result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, 1).ToArray();

        Assert.AreEqual(1, result.Length);
        var (pos, length, minMult, maxMult) = result[0];
        Console.WriteLine(result[0]);
        Assert.AreEqual(0, pos.Value);
        Assert.AreEqual(0, pos.CharPosition);
        Assert.AreEqual(2, length);
        Assert.IsTrue(minMult < 1 && 1 < maxMult);

        var result2 = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, 3).ToArray();

        Assert.AreEqual(0, result2.Length);
    }

    [Test]
    public void TestDynamicDistanceRetrieveSubstringsDynamicLengthAndDistanceRange() {
        var query = new[] {
            new RhythmToken(0, 0, 0),
            new RhythmToken(0, 4, 20),
            new RhythmToken(0, 4, 40),
            new RhythmToken(1, 8, 40),
            new RhythmToken(0, 2, 10),
            new RhythmToken(0, 4, 40)
        }.AsMemory();

        var result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, 1).ToArray();

        Assert.AreEqual(1, result.Length);
        var (pos, length, minMult, maxMult) = result[0];
        Console.WriteLine(result[0]);
        Assert.AreEqual(0, pos.Value);
        Assert.AreEqual(0, pos.CharPosition);
        Assert.AreEqual(6, length);
        Assert.IsTrue(minMult < 0.5 && 0.5 < maxMult);
    }

    [Test]
    [Explicit]
    public void TestRhythmTokenSize() {
        Console.WriteLine(Marshal.SizeOf<RhythmToken>());
    }
}