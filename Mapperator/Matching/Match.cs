using Mapperator.Model;
using TrieNet;

namespace Mapperator.Matching;

public readonly struct Match {
    /// <summary>
    /// The whole sequence of matching data points
    /// </summary>
    public readonly ReadOnlyMemory<MapDataPoint> Sequence;

    /// <summary>
    /// The position of the start of the sequence in the data.
    /// </summary>
    public readonly WordPosition<int> SeqPos;

    /// <summary>
    /// The length of the matched sequence.
    /// </summary>
    public int Length => Sequence.Length;

    /// <summary>
    /// The minimum allowed distance scalar for this match.
    /// </summary>
    public readonly double MinMult;

    /// <summary>
    /// The maximum allowed distance scalar for this match.
    /// </summary>
    public readonly double MaxMult;

    public Match(ReadOnlyMemory<MapDataPoint> sequence, WordPosition<int> seqPos, double minMult, double maxMult) {
        Sequence = sequence;
        SeqPos = seqPos;
        MinMult = minMult;
        MaxMult = maxMult;
    }
}