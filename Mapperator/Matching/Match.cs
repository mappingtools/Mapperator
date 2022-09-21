using Gma.DataStructures.StringSearch;
using Mapperator.Model;

namespace Mapperator.Matching;

public struct Match {
    /// <summary>
    /// The whole sequence of matching data points
    /// </summary>
    public ReadOnlyMemory<MapDataPoint> WholeSequence;

    /// <summary>
    /// The number of lookback elements in the whole sequence.
    /// This is also the index of the first desired data point.
    /// </summary>
    public readonly int Lookback;

    /// <summary>
    /// The position of the start of the whole sequence in the data.
    /// </summary>
    public WordPosition<int> SeqPos;

    public WordPosition<int> WantedPos => new(SeqPos.CharPosition + Lookback, SeqPos.Value);

    public Match(ReadOnlyMemory<MapDataPoint> wholeSequence, int lookback, WordPosition<int> seqPos) {
        WholeSequence = wholeSequence;
        Lookback = lookback;
        SeqPos = seqPos;
    }
}