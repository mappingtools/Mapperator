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
        trie = new RhythmDistanceTrie { DistLeniencyFactor = 0.5f, DistLeniency = 3 };
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

        var result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, new RhythmDistanceTrie.MinLengthProvider(1)).ToArray();

        Assert.That(1, Is.EqualTo(result.Length));
        var (pos, length, minMult, maxMult) = result[0];
        Console.WriteLine(result[0]);
        Assert.That(0, Is.EqualTo(pos.Value));
        Assert.That(0, Is.EqualTo(pos.CharPosition));
        Assert.That(7, Is.EqualTo(length));
        Assert.That(minMult < 1 && 1 < maxMult);
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

        var result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, new RhythmDistanceTrie.MinLengthProvider(1))
            .OrderBy(o => o.Item1.CharPosition).ToArray();

        Assert.That(5, Is.EqualTo(result.Length));
        var (pos, length, minMult, maxMult) = result[0];
        Assert.That(1, Is.EqualTo(pos.CharPosition));
        Assert.That(6, Is.EqualTo(length));
        Assert.That(minMult < 1 && 1 < maxMult);
        (pos, length, minMult, maxMult) = result[1];
        Assert.That(2, Is.EqualTo(pos.CharPosition));
        Assert.That(2, Is.EqualTo(length));
        Assert.That(minMult < 0.6 && 0.6 < maxMult);
        (pos, length, minMult, maxMult) = result[2];
        Assert.That(3, Is.EqualTo(pos.CharPosition));
        Assert.That(1, Is.EqualTo(length));
        Assert.That(minMult < 0.5 && 0.5 < maxMult);
        (pos, length, minMult, maxMult) = result[3];
        Assert.That(5, Is.EqualTo(pos.CharPosition));
        Assert.That(2, Is.EqualTo(length));
        Assert.That(minMult < 1 && 1 < maxMult);
        (pos, length, minMult, maxMult) = result[4];
        Assert.That(6, Is.EqualTo(pos.CharPosition));
        Assert.That(1, Is.EqualTo(length));
        Assert.That(minMult < 1 && 1 < maxMult);

        result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, new RhythmDistanceTrie.MinLengthProvider(2))
            .OrderBy(o => o.Item1.CharPosition).ToArray();

        Assert.That(3, Is.EqualTo(result.Length));
        (pos, length, _, _) = result[0];
        Assert.That(1, Is.EqualTo(pos.CharPosition));
        Assert.That(6, Is.EqualTo(length));
        (pos, length, _, _) = result[1];
        Assert.That(2, Is.EqualTo(pos.CharPosition));
        Assert.That(2, Is.EqualTo(length));
        (pos, length, _, _) = result[2];
        Assert.That(5, Is.EqualTo(pos.CharPosition));
        Assert.That(2, Is.EqualTo(length));

        result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, new RhythmDistanceTrie.MinLengthProvider(3)).ToArray();

        Assert.That(1, Is.EqualTo(result.Length));
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

        var result = trie.RetrieveSubstringsDynamicLengthAndDistanceRange(query, new RhythmDistanceTrie.MinLengthProvider(1)).ToArray();

        Assert.That(1, Is.EqualTo(result.Length));
        var (pos, length, minMult, maxMult) = result[0];
        Console.WriteLine(result[0]);
        Assert.That(0, Is.EqualTo(pos.Value));
        Assert.That(0, Is.EqualTo(pos.CharPosition));
        Assert.That(7, Is.EqualTo(length));
        Assert.That(minMult < 0.5 && 0.5 < maxMult);
    }

    [Test]
    [Explicit]
    public void TestRhythmTokenSize() {
        Console.WriteLine(Marshal.SizeOf<RhythmToken>());
    }
}