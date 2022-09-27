using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching.DataStructures;

public readonly struct RhythmToken : IEquatable<RhythmToken>, IComparable<RhythmToken> {
    const int gapResolution = 6;
    const int gapRange = 9;

    public byte Type { get; }

    public byte Dist { get; }

    public int Gap => Type % gapRange;

    public int DataType2 => Type / gapRange;

    public RhythmToken(byte type, byte dist) {
        Type = type;
        Dist = dist;
    }

    public RhythmToken(MapDataPoint mapDataPoint) {
        Dist = (byte) MathHelper.Clamp(mapDataPoint.Spacing / 4, 0, 255);
        var gap = MathHelper.Clamp((int) Math.Round(Math.Log2(mapDataPoint.BeatsSince) + gapResolution), 0, gapRange - 1);

        if (mapDataPoint.DataType == DataType.Release) {
            // The beat gap is less important for selecting sliders so lets reduce gap to just 2 classes for sliders
            // If its less than 1/2 beat, its 0
            gap = gap < 5 ? 0 : gap > 5 ? 2 : 1;
            // Lets also allow a coarser range of distances
            Dist = (byte) MathHelper.Clamp(mapDataPoint.Spacing / 12, 0, 255);
        }

        Type = (byte)(mapDataPoint.DataType switch {
            DataType.Hit => gap,
            DataType.Spin => gapRange + gap,
            DataType.Release => mapDataPoint.Repeats switch {
                0 => gapRange * 2 + gap,
                1 => gapRange * 3 + gap,
                _ => gapRange * 4 + gap
            },
            _ => gap
        });
    }

    public bool Equals(RhythmToken other) {
        return Type == other.Type && Dist == other.Dist;
    }

    public override bool Equals(object? obj) {
        return obj is RhythmToken other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Type, Dist);
    }

    public int CompareTo(RhythmToken other) {
        var typeComparison = Type.CompareTo(other.Type);
        if (typeComparison != 0) return typeComparison;
        return Dist.CompareTo(other.Dist);
    }

    public static bool operator ==(RhythmToken left, RhythmToken right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RhythmToken left, RhythmToken right)
    {
        return !(left == right);
    }
}