using Mapperator.Model;

namespace Mapperator.Matching;

public struct Match {
    /// <summary>
    /// The whole sequence of matching data points
    /// </summary>
    public ReadOnlyMemory<MapDataPoint> Seq;

    /// <summary>
    /// The index in <see cref="Seq"/> of the desired data point.
    /// </summary>
    public int Pos;

    /// <summary>
    /// The ID of the desired data point.
    /// This is sequential so the ID of the next element in <see cref="Seq"/> is <see cref="Id"/> + 1.
    /// </summary>
    public int Id;

    public Match(ReadOnlyMemory<MapDataPoint> seq, int pos, int id) {
        Seq = seq;
        Pos = pos;
        Id = id;
    }
}