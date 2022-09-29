namespace Mapperator.Matching.DataStructures;

public readonly struct RhythmToken : IEquatable<RhythmToken>, IComparable<RhythmToken> {

    private readonly ushort data;

    public int Dist => data & 0b0000000011111111;

    public int Type => (data & 0b1111000000000000) >> 12;

    public int Gap => (data & 0b0000111100000000) >> 8;

    /// <summary>
    /// Type and gap must be less than 16.
    /// Dist must be less than 256.
    /// </summary>
    public RhythmToken(int type, int gap, int dist) {
        data = (ushort)(type << 12 | gap << 8 | dist);
    }

    public bool Equals(RhythmToken other) {
        return data == other.data;
    }

    public override bool Equals(object? obj) {
        return obj is RhythmToken other && Equals(other);
    }

    public override int GetHashCode() {
        return data;
    }

    public int CompareTo(RhythmToken other) {
        return data.CompareTo(other.data);
    }

    public static bool operator ==(RhythmToken left, RhythmToken right) {
        return left.Equals(right);
    }

    public static bool operator !=(RhythmToken left, RhythmToken right) {
        return !(left == right);
    }

    public static bool operator <=(RhythmToken left, RhythmToken right) {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(RhythmToken left, RhythmToken right) {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator <(RhythmToken left, RhythmToken right) {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(RhythmToken left, RhythmToken right) {
        return left.CompareTo(right) > 0;
    }
}