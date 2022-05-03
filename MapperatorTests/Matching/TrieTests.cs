using Gma.DataStructures.StringSearch;
using Mapperator.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapperatorTests.Matching {
    public class TrieTests {
        private readonly List<List<MapDataPoint>> mapDataPoints = new();
        private readonly UkkonenTrie<byte, int> rhythmTrie = new(1);

        [OneTimeSetUp]
        public void Setup() {
            var mapString = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 2, 3, 5, 7, 11, 13, 17, 23, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            rhythmTrie.Add(mapString.AsMemory(), 0);
        }

        [TestCase(new byte[] { 1 }, new int[] { 0, 18, 27, 28, 29, 30, 31, 32, 33, 34, 35 })]
        [TestCase(new byte[] { 1, 2 }, new int[] { 0, 18 })]
        [TestCase(new byte[] { 1, 2, 3 }, new int[] { 0, 18 })]
        [TestCase(new byte[] { 1, 2, 3, 4 }, new int[] { 0 })]
        [TestCase(new byte[] { 1, 2, 3, 5 }, new int[] { 18 })]
        [TestCase(new byte[] { 11 }, new int[] { 23 })]
        [TestCase(new byte[] { 11, 13 }, new int[] { 23 })]
        [TestCase(new byte[] { 11, 13, 17 }, new int[] { 23 })]
        [TestCase(new byte[] { 9 }, new int[] { 8, 10 })]
        [TestCase(new byte[] { 9, 10 }, new int[] { 8 })]
        [TestCase(new byte[] { 9, 8 }, new int[] { 10 })]
        [TestCase(new byte[] { 9, 10, 11 }, new int[] { })]
        public void TestSubstrings(byte[] pattern, int[] positions) {
            var result = rhythmTrie.RetrieveSubstrings(pattern).ToList();
            CollectionAssert.AreEquivalent(positions, result.Select(o => (int) o.CharPosition).ToArray());
        }
    }
}