using TrieNet;
using TrieNet.Ukkonen;

namespace Mapperator.Matching.DataStructures;

public class RhythmDistanceTrie : UkkonenTrie<RhythmToken, int> {
    public RhythmDistanceTrie(int minSuffixLength) : base(minSuffixLength) { }

    /// <summary>
    /// Finds all patterns which can match all or part of the word after scaling the distance.
    /// </summary>
    /// <param name="word"></param>
    /// <param name="minLength">The minimum match length. If 0, this returns all of the trie data.</param>
    /// <returns>Word positions and matching length</returns>
    public IEnumerable<(WordPosition<int>, int, float, float)> RetrieveSubstringsDynamicLengthAndDistanceRange(ReadOnlyMemory<RhythmToken> word, int minLength) {
        if (word.Length < minLength) yield break;
        // Perform a breadth-first search
        // Remember a min and max multiplier which would keep the whole sequence in distance margin
        var nodesToSearch = new Queue<(Node<RhythmToken, int>, int, float, float)>();
        nodesToSearch.Enqueue((Root, 0, float.NegativeInfinity, float.PositiveInfinity));
        while (nodesToSearch.Count > 0) {
            var (currentNode, i, minMult, maxMult) = nodesToSearch.Dequeue();
            var charToMatch = word.Span[i];

            // Yield all Data of this node (not the whole subtree)
            if (i >= minLength) {
                foreach (var e in currentNode.Data) {
                    yield return (e, i, minMult, maxMult);
                }
            }

            // follow all the EdgeA<T> which are valid
            var edges = currentNode.Edges;
            foreach (var (ch, edge) in edges) {
                if (edge is null || ch.Type != charToMatch.Type || ch.Gap != charToMatch.Gap) continue;

                // Check how much of the edge label somewhat matches the word
                var label = edge.Label.Span;
                var lenToMatch = Math.Min(word.Length - i, label.Length);
                var edgeMinMult = minMult;
                var edgeMaxMult = maxMult;
                int matchingLen;
                for (matchingLen = 0; matchingLen < lenToMatch; matchingLen++) {
                    var charToMatch2 = word.Span[i + matchingLen];
                    var chMin2 = Math.Max(charToMatch.Dist * 0.66f - 3, 0);
                    var chMax2 = Math.Min(charToMatch.Dist * 1.5f + 3, 255);

                    var ch2 = label[i];

                    if (ch2.Type != charToMatch2.Type || ch2.Gap != charToMatch2.Gap) break;

                    var dist = ch2.Dist;
                    var newMinMult = chMin2 / dist;
                    var newMaxMult = chMax2 / dist;

                    if (newMinMult > edgeMaxMult || newMaxMult < edgeMinMult) break;

                    edgeMinMult = Math.Max(newMinMult, edgeMinMult);
                    edgeMaxMult = Math.Min(newMaxMult, edgeMaxMult);
                }

                // All yields must be at least as long as minLength. That prevents us from straight up yielding the whole trie

                // If len is less than lenToMatch, then yield whole subtree of target and break, but limited in length
                // If len is equal to lenToMatch and we've exhausted the query, then yield the whole subtree of the target and break
                if (matchingLen < lenToMatch || (matchingLen == lenToMatch && label.Length + i >= word.Length)) {
                    if (matchingLen + i >= minLength) {
                        foreach (var e in edge.Target.GetData()) {
                            yield return (e, i + matchingLen, edgeMinMult, edgeMaxMult);
                        }
                    }
                    continue;
                }

                // If len is equal to lenToMatch and we can go further, then enqueue the target which will eventually yield the whole subtree of the target
                nodesToSearch.Enqueue((edge.Target, i + lenToMatch, edgeMinMult, edgeMaxMult));
            }
        }
    }

    public IEnumerable<WordPosition<int>> RetrieveSubstringsRangeDepthFirst(ReadOnlyMemory<RhythmToken> min, ReadOnlyMemory<RhythmToken> max) {
        if (min.Length != max.Length) throw new ArgumentException("Lengths of min and max must be the same.");
        if (min.Length < MinSuffixLength) return Enumerable.Empty<WordPosition<int>>();
        var nodes = SearchNodeRangeDepthFirst(Root, min, max);
        return nodes.SelectMany(o => o.GetData());
    }

    private static IEnumerable<Node<RhythmToken, int>> SearchNodeRangeDepthFirst(Node<RhythmToken, int> startNode,
        ReadOnlyMemory<RhythmToken> min,
        ReadOnlyMemory<RhythmToken> max) {
        /*
         * Verifies if exists a path from the root to a Node such that the concatenation
         * of all the labels on the path is a superstring of the given word.
         * If such a path is found, the last Node on it is returned.
         */

        // Perform a depth-first search
        // Store the next node and edge index that should be searched (node, i, edge index)
        var nodesToSearch = new Stack<(Node<RhythmToken, int>, int, int)>();
        nodesToSearch.Push((startNode, 0, 0));
        while (nodesToSearch.Count > 0) {
            var (currentNode, i, edgeIndex) = nodesToSearch.Pop();
            search:
            var chMin = min.Span[i];
            var chMax = max.Span[i];
            var edges = currentNode.Edges;
            var n = edges.Count;

            // Find the next valid edge from edgeIndex
            for (var j = edgeIndex; j < n; j++) {
                var (ch, edge) = edges[j];

                if (ch.CompareTo(chMin) < 0 || ch.CompareTo(chMax) > 0) continue;

                var label = edge.Label.Span;
                var lenToMatch = Math.Min(min.Length - i, label.Length);

                if (!RegionMatchesRange(min.Span, max.Span, i, label, 0, lenToMatch))
                    // the label on the EdgeA<T> does not correspond to the one in the string to search
                    continue;

                // We found a valid edge
                if (label.Length >= min.Length - i)
                    yield return edge.Target;
                else {
                    // advance to next Node
                    nodesToSearch.Push((currentNode, i, j + 1));
                    (currentNode, i, edgeIndex) = (edge.Target, i + lenToMatch, 0);
                    goto search;
                }
            }
        }
    }

    private static bool RegionMatchesRange(ReadOnlySpan<RhythmToken> min, ReadOnlySpan<RhythmToken> max, int toffset,
        ReadOnlySpan<RhythmToken> second, int ooffset, int len) {
        for (var i = 0; i < len; i++) {
            var chMin = min[toffset + i];
            var chMax = max[toffset + i];
            var two = second[ooffset + i];
            if (two.CompareTo(chMin) < 0 || two.CompareTo(chMax) > 0) return false;
        }

        return true;
    }
}