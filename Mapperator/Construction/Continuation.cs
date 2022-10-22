using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Construction;

public readonly struct Continuation {
    public readonly Vector2 Pos;

    public readonly double Angle;

    public readonly double Time;

    public Continuation(Vector2 pos, double angle, double time) {
        Time = time;
        Angle = angle;
        Pos = pos;
    }

    public void Deconstruct(out Vector2 pos, out double angle, out double time) {
        pos = Pos;
        angle = Angle;
        time = Time;
    }
}