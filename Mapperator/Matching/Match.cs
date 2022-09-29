using Mapperator.Model;
using TrieNet;

namespace Mapperator.Matching;

public readonly struct Match {
    /// <summary>
    /// The whole sequence of matching data points
    /// </summary>
    public readonly ReadOnlyMemory<MapDataPoint> WholeSequence;

    /// <summary>
    /// The number of lookback elements in the whole sequence.
    /// This is also the index of the first desired data point.
    /// </summary>
    public readonly int Lookback;

    /// <summary>
    /// The position of the start of the whole sequence in the data.
    /// </summary>
    public readonly WordPosition<int> SeqPos;

    public WordPosition<int> WantedPos => new(SeqPos.CharPosition + Lookback, SeqPos.Value);

    /// <summary>
    /// The length of the matched sequence without look-back.
    /// </summary>
    public int Length => WholeSequence.Length - Lookback;

    public Match(ReadOnlyMemory<MapDataPoint> wholeSequence, int lookback, WordPosition<int> seqPos) {
        WholeSequence = wholeSequence;
        Lookback = lookback;
        SeqPos = seqPos;
    }
}