namespace Mapperator.Matching.DataStructures;

public readonly struct RhythmToken : IEquatable<RhythmToken>, IComparable<RhythmToken> {

    private readonly byte typeGap;

    public byte Dist { get; }

    public int Type => (typeGap & 0b11110000) >> 4;

    public int Gap => typeGap & 0b00001111;

    /// <summary>
    /// Type and gap must be less than 16.
    /// Dist must be less than 256.
    /// </summary>
    public RhythmToken(int type, int gap, int dist) {
        typeGap = (byte)(type << 4 | gap);
        Dist = (byte)dist;
    }

    public bool Equals(RhythmToken other) {
        return typeGap == other.typeGap && Dist == other.Dist;
    }

    public override bool Equals(object? obj) {
        return obj is RhythmToken other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(typeGap, Dist);
    }

    public int CompareTo(RhythmToken other) {
        var typeComparison = typeGap.CompareTo(other.typeGap);
        if (typeComparison != 0) return typeComparison;
        return Dist.CompareTo(other.Dist);
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