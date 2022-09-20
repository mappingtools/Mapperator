using Gma.DataStructures.StringSearch;
using Mapperator.Model;

namespace Mapperator.Matching;

public struct Match {
    /// <summary>
    /// The whole sequence of matching data points
    /// </summary>
    public ReadOnlyMemory<MapDataPoint> Seq;

    /// <summary> sired data point.
    /// </summary>
    public readonly int WantedIndex;

    /// <summary>
    /// The position of the sequence in the data.
    /// </summary>
    public WordPosition<int> SeqPos;

    public WordPosition<int> WantedPos => new(SeqPos.CharPosition + WantedIndex, SeqPos.Value);

    public Match(ReadOnlyMemory<MapDataPoint> seq, int wantedIndex, WordPosition<int> seqPos) {
        Seq = seq;
        WantedIndex = wantedIndex;
        SeqPos = seqPos;
    }
}