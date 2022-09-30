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
            new RhythmToken(0, 4, 40),
            new RhythmToken(0, 4, 80),
            new RhythmToken(1, 8, 80),
            new RhythmToken(0, 4, 40),
            new RhythmToken(0, 4, 40)
        }.AsMemory(), 0);
    }

    [Test]
    public void TestRetrieveSubstringsDynamicLengthAndDistanceRange() {
        var query = new[] {
            new RhythmToken(0, 0, 0),
            new RhythmToken(0, 4, 40),
            new RhythmToken(0, 4, 40),
            new RhythmToken(0, 4, 80),
            new RhythmToken(1, 8, 80),
            new RhythmToken(0, 4, 40),
            new RhythmToken(0, 4, 40)
        }.AsMemory();

        var result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, 1).ToArray();

        Assert.AreEqual(1, result.Length);
        var (pos, length, minMult, maxMult) = result[0];
        Console.WriteLine(result[0]);
        Assert.AreEqual(0, pos.Value);
        Assert.AreEqual(0, pos.CharPosition);
        Assert.AreEqual(7, length);
        Assert.IsTrue(minMult < 1 && 1 < maxMult);
    }

    [Test]
    public void TestDynamicLengthRetrieveSubstringsDynamicLengthAndDistanceRange() {
        var query = new[] {
            new RhythmToken(0, 4, 40),
            new RhythmToken(0, 4, 40),
            new RhythmToken(0, 4, 80),
            new RhythmToken(1, 8, 80),
            new RhythmToken(0, 4, 40),
            new RhythmToken(0, 4, 40)
        }.AsMemory();

        var result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, 1)
            .OrderBy(o => o.Item1.CharPosition).ToArray();

        Assert.AreEqual(5, result.Length);
        var (pos, length, minMult, maxMult) = result[0];
        Assert.AreEqual(1, pos.CharPosition);
        Assert.AreEqual(6, length);
        Assert.IsTrue(minMult < 1 && 1 < maxMult);
        (pos, length, minMult, maxMult) = result[1];
        Assert.AreEqual(2, pos.CharPosition);
        Assert.AreEqual(2, length);
        Assert.IsTrue(minMult < 0.6 && 0.6 < maxMult);
        (pos, length, minMult, maxMult) = result[2];
        Assert.AreEqual(3, pos.CharPosition);
        Assert.AreEqual(1, length);
        Assert.IsTrue(minMult < 0.5 && 0.5 < maxMult);
        (pos, length, minMult, maxMult) = result[3];
        Assert.AreEqual(5, pos.CharPosition);
        Assert.AreEqual(2, length);
        Assert.IsTrue(minMult < 1 && 1 < maxMult);
        (pos, length, minMult, maxMult) = result[4];
        Assert.AreEqual(6, pos.CharPosition);
        Assert.AreEqual(1, length);
        Assert.IsTrue(minMult < 1 && 1 < maxMult);

        result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, 2)
            .OrderBy(o => o.Item1.CharPosition).ToArray();

        Assert.AreEqual(3, result.Length);
        (pos, length, _, _) = result[0];
        Assert.AreEqual(1, pos.CharPosition);
        Assert.AreEqual(6, length);
        (pos, length, _, _) = result[1];
        Assert.AreEqual(2, pos.CharPosition);
        Assert.AreEqual(2, length);
        (pos, length, _, _) = result[2];
        Assert.AreEqual(5, pos.CharPosition);
        Assert.AreEqual(2, length);

        result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, 3).ToArray();

        Assert.AreEqual(1, result.Length);
    }

    [Test]
    public void TestDynamicDistanceRetrieveSubstringsDynamicLengthAndDistanceRange() {
        var query = new[] {
            new RhythmToken(0, 0, 0),
            new RhythmToken(0, 4, 20),
            new RhythmToken(0, 4, 20),
            new RhythmToken(0, 4, 40),
            new RhythmToken(1, 8, 40),
            new RhythmToken(0, 4, 20),
            new RhythmToken(0, 4, 20)
        }.AsMemory();

        var result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, 1).ToArray();

        Assert.AreEqual(1, result.Length);
        var (pos, length, minMult, maxMult) = result[0];
        Console.WriteLine(result[0]);
        Assert.AreEqual(0, pos.Value);
        Assert.AreEqual(0, pos.CharPosition);
        Assert.AreEqual(7, length);
        Assert.IsTrue(minMult < 0.5 && 0.5 < maxMult);
    }

    [Test]
    [Explicit]
    public void TestRhythmTokenSize() {
        Console.WriteLine(Marshal.SizeOf<RhythmToken>());
    }
}