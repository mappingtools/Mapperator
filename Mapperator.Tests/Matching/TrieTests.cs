using Gma.DataStructures.StringSearch;
using NUnit.Framework;
using System;
using System.Linq;

namespace Mapperator.Tests.Matching {
    public class TrieTests {
        private readonly UkkonenTrie<byte, int> rhythmTrie = new(1);

        [OneTimeSetUp]
        public void Setup() {
            var mapString = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 2, 3, 5, 7, 11, 13, 17, 23, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            rhythmTrie.Add(mapString.AsMemory(), 0);
        }

        [TestCase(new byte[] { 1 }, new[] { 0, 18, 27, 28, 29, 30, 31, 32, 33, 34, 35 })]
        [TestCase(new byte[] { 1, 2 }, new[] { 0, 18 })]
        [TestCase(new byte[] { 1, 2, 3 }, new[] { 0, 18 })]
        [TestCase(new byte[] { 1, 2, 3, 4 }, new[] { 0 })]
        [TestCase(new byte[] { 1, 2, 3, 5 }, new[] { 18 })]
        [TestCase(new byte[] { 11 }, new[] { 23 })]
        [TestCase(new byte[] { 11, 13 }, new[] { 23 })]
        [TestCase(new byte[] { 11, 13, 17 }, new[] { 23 })]
        [TestCase(new byte[] { 9 }, new[] { 8, 10 })]
        [TestCase(new byte[] { 9, 10 }, new[] { 8 })]
        [TestCase(new byte[] { 9, 8 }, new[] { 10 })]
        [TestCase(new byte[] { 9, 10, 11 }, new int[] { })]
        public void TestSubstrings(byte[] pattern, int[] positions) {
            var result = rhythmTrie.RetrieveSubstrings(pattern).ToList();
            CollectionAssert.AreEquivalent(positions, result.Select(o => o.CharPosition).ToArray());
        }
    }
}