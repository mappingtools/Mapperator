using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator;

public class DistanceAnalyser {
    private const int Bins = 512;
    private const int AngleBins = 629;

    public double[] ExtractSliderAngles(IEnumerable<IBeatmap> beatmaps) {
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

    public double[] ExtractVisualSpacing(IEnumerable<IBeatmap> beatmaps) {
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

    public int[] ExtractVisualSpacing(IBeatmap beatmap) {
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
                var maxDist = CalculateMaxDist(neighbourIndex, hitObjectIndex, beatmap, hoPoints);

                var dist = ShortestDistance(hoPoints[hitObject], hoPoints[neighbour]);
                var distClass = (int)Math.Round(dist / radius * 100);
                if (distClass is < Bins and >= 0 && dist * 1.5 < maxDist)
                    spacings[distClass]++;
            }
        }

        return spacings;
    }

    private static double CalculateMaxDist(int neighbourIndex, int hitObjectIndex, IBeatmap beatmap, Dictionary<HitObject, Vector2[]> points) {
        var maxDist = 0d;
        if (neighbourIndex < hitObjectIndex) {
            var lastPos = points[beatmap.HitObjects[neighbourIndex]][^1];
            for (var i = neighbourIndex + 1; i < hitObjectIndex; i++) {
                var curr = points[beatmap.HitObjects[i]];
                foreach (var p in curr) {
                    maxDist += Vector2.Distance(lastPos, p);
                    lastPos = p;
                }
            }
            maxDist += Vector2.Distance(lastPos, points[beatmap.HitObjects[hitObjectIndex]][0]);
        }
        else {
            var lastPos = points[beatmap.HitObjects[hitObjectIndex]][^1];
            for (var i = hitObjectIndex + 1; i < neighbourIndex; i++) {
                var curr = points[beatmap.HitObjects[i]];
                foreach (var p in curr) {
                    maxDist += Vector2.Distance(lastPos, p);
                    lastPos = p;
                }
            }
            maxDist += Vector2.Distance(lastPos, points[beatmap.HitObjects[neighbourIndex]][0]);
        }

        return maxDist;
    }

    private static Dictionary<HitObject, Vector2[]> GetHitObjectsAsPoints(IEnumerable<HitObject> hitObjects) {
        var hoPoints = new Dictionary<HitObject, Vector2[]>();
        foreach (var hitObject in hitObjects) {
            if (hitObject is not HitCircle && hitObject is not Slider) continue;

            hoPoints[hitObject] = hitObject switch {
                Slider s => GetSliderPoints(s),
                _ => new[]{ hitObject.Pos }
            };
        }

        return hoPoints;
    }

    private static Vector2[] GetSliderPoints(Slider s) {
        const int pointCount = 100;
        var path = s.GetSliderPath();
        var points = new Vector2[pointCount];
        for (var i = 0; i < pointCount; i++) {
            points[i] = path.PositionAt((double)i / (pointCount - 1));
        }

        return points;
    }

    private static double ShortestDistance(Vector2[] a, Vector2[] b) {
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