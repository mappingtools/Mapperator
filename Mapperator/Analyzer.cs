using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator;

public static class Analyzer {
    private const int Bins = 512;
    private const int AngleBins = 629;

    public static double[] ExtractSliderAngles(IEnumerable<IBeatmap> beatmaps) {
        var angles = new int[AngleBins];
        foreach (var beatmap in beatmaps) {
            beatmap.CalculateEndPositions();
            foreach (var hitObject in beatmap.HitObjects) {
                if (hitObject is not Slider s) continue;
                if (s.PixelLength > 1.5 * Vector2.Distance(s.Pos, s.EndPos)) continue;

                var angle = (s.EndPos - s.Pos).Theta;
                var angleClass = MathHelper.Clamp((int)Math.Round((angle + Math.PI) * 100), 0, AngleBins - 1);
                angles[angleClass]++;
            }
        }

        var normalizedAngles = new double[AngleBins];
        var sum = angles.Sum();
        for (var i = 0; i < AngleBins; i++) {
            normalizedAngles[i] = (double)angles[i] / sum * AngleBins;
        }

        return normalizedAngles;
    }

    public static double[] ExtractVisualSpacing(IEnumerable<IBeatmap> beatmaps) {
        var spacings = new int[Bins];
        foreach (var beatmap in beatmaps) {
            var beatmapSpacings = ExtractVisualSpacing(beatmap);
            for (var i = 0; i < Bins; i++) {
                spacings[i] += beatmapSpacings[i];
            }
        }

        var normalizedSpacings = new double[Bins];
        var sum = spacings.Sum();
        for (var i = 0; i < Bins; i++) {
            normalizedSpacings[i] = (double)spacings[i] / sum * Bins;
        }

        return normalizedSpacings;
    }

    public static int[] ExtractVisualSpacing(IBeatmap beatmap) {
        var spacings = new int[Bins];
        var radius = beatmap.Difficulty.HitObjectRadius;
        var margin = beatmap.Difficulty.ApproachTime;
        var hoPoints = GetHitObjectsAsPoints(beatmap.HitObjects);

        foreach (var hitObject in beatmap.HitObjects) {
            if (hitObject is not HitCircle && hitObject is not Slider) continue;

            var hitObjectIndex = beatmap.HitObjects.IndexOf(hitObject);

            // Get all neighbouring hit objects
            var neighbours = beatmap.GetHitObjectsWithRangeInRange(hitObject.StartTime - margin, hitObject.EndTime + margin);
            neighbours.Remove(hitObject);

            foreach (var neighbour in neighbours) {
                if (neighbour is not HitCircle && neighbour is not Slider) continue;

                var neighbourIndex = beatmap.HitObjects.IndexOf(neighbour);
                var maxDist = CalculateMaxDist(neighbourIndex, hitObjectIndex, beatmap.HitObjects, hoPoints);

                var dist = ShortestDistance(hoPoints[hitObject], hoPoints[neighbour]);
                var distClass = (int)Math.Round(dist / radius * 100);
                if (distClass is < Bins and >= 0 && dist * 1.5 < maxDist)
                    spacings[distClass]++;
            }
        }

        return spacings;
    }

    public static double CalculateMaxDist(int neighbourIndex, int hitObjectIndex, IList<HitObject> hitObjects, Dictionary<HitObject, Vector2[]> points) {
        var maxDist = 0d;
        if (neighbourIndex < hitObjectIndex) {
            var lastPos = points[hitObjects[neighbourIndex]][^1];
            for (var i = neighbourIndex + 1; i < hitObjectIndex; i++) {
                var curr = points[hitObjects[i]];
                foreach (var p in curr) {
                    maxDist += Vector2.Distance(lastPos, p);
                    lastPos = p;
                }
            }
            maxDist += Vector2.Distance(lastPos, points[hitObjects[hitObjectIndex]][0]);
        }
        else {
            var lastPos = points[hitObjects[hitObjectIndex]][^1];
            for (var i = hitObjectIndex + 1; i < neighbourIndex; i++) {
                var curr = points[hitObjects[i]];
                foreach (var p in curr) {
                    maxDist += Vector2.Distance(lastPos, p);
                    lastPos = p;
                }
            }
            maxDist += Vector2.Distance(lastPos, points[hitObjects[neighbourIndex]][0]);
        }

        return maxDist;
    }

    public static Dictionary<HitObject, Vector2[]> GetHitObjectsAsPoints(IEnumerable<HitObject> hitObjects) {
        var hoPoints = new Dictionary<HitObject, Vector2[]>();
        foreach (var hitObject in hitObjects) {
            if (hitObject is not HitCircle && hitObject is not Slider) continue;

            hoPoints[hitObject] = GetHitObjectAsPoints(hitObject);
        }

        return hoPoints;
    }

    public static Vector2[] GetHitObjectAsPoints(HitObject hitObject) {
        return hitObject switch {
            Slider s => GetSliderPoints(s),
            _ => new[]{ hitObject.Pos }
        };
    }

    public static Vector2[] GetSliderPoints(Slider s) {
        const int pointCount = 100;
        var path = s.GetSliderPath();
        var points = new Vector2[pointCount];
        for (var i = 0; i < pointCount; i++) {
            points[i] = path.PositionAt((double)i / (pointCount - 1));
        }

        return points;
    }

    public static double ShortestDistance(Vector2[] a, Vector2[] b) {
        var minDist = double.PositiveInfinity;
        for (var i = 0; i < a.Length; i++) {
            for (var j = 0; j < b.Length; j++) {
                var dist = Vector2.Distance(a[i], b[j]);
                minDist = Math.Min(minDist, dist);
            }
        }

        return minDist;
    }
}