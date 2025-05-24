using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Construction;

public readonly struct Continuation {
    public readonly Vector2 Pos;

    public readonly double Angle;

    public readonly double Time;

    public Continuation(Vector2 pos, double angle, double time) {
        Pos = pos;
        Angle = angle;
        Time = time;
    }

    public void Deconstruct(out Vector2 pos, out double angle, out double time) {
        pos = Pos;
        angle = Angle;
        time = Time;
    }

    /// <summary>
    /// Returns the continuation at the end of the hitobjects.
    /// </summary>
    public Continuation(IList<HitObject> hitObjects) {
        if (hitObjects.Count == 0) {
            Pos = new Vector2(256, 192);
            Angle = 0;
            Time = 0;
            return;
        }

        var lastPos = hitObjects[^1].EndPos;

        var beforeLastPos = new Vector2(256, 192);
        for (var i = hitObjects.Count - 1; i >= 0; i--) {
            var ho = hitObjects[i];

            if (Vector2.DistanceSquared(ho.EndPos, lastPos) > Precision.DOUBLE_EPSILON) {
                beforeLastPos = ho.EndPos;
                break;
            }

            if (Vector2.DistanceSquared(ho.Pos, lastPos) > Precision.DOUBLE_EPSILON) {
                beforeLastPos = ho.Pos;
                break;
            }
        }

        Pos = lastPos;
        Angle = Vector2.DistanceSquared(beforeLastPos, lastPos) > Precision.DOUBLE_EPSILON
            ? (lastPos - beforeLastPos).Theta
            : 0;
        Time = hitObjects[^1].EndTime;
    }
}