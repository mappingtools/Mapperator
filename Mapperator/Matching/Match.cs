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

    public Match Next() {
        if (Sequence.Length < 2) throw new Exception(@"Can't generate match with next data point, because it would contain no elements.");

        return new Match(Sequence[1..], new WordPosition<int>(SeqPos.CharPosition + 1, SeqPos.Value), MinMult, MaxMult);
    }
}