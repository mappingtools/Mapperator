using Mapperator.Construction;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Contexts;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools_Core.BeatmapHelper.IO.Decoding.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.ToolHelpers;
using Tensorflow.Keras.Engine;
using Tensorflow.NumPy;

namespace Mapperator.ML;

using static Tensorflow.Binding;
using Tensorflow;

public class MapperatorML {
    private readonly IModel model;
    private readonly HitObjectDecoder decoder;
    private readonly double lookBack;
    private readonly int numWindows;
    private readonly float maxSdfDistance;
    private readonly int width;
    private readonly int height;
    private readonly int flatNum;
    private readonly Tensor coordinates;
    private readonly Tensor coordinatesFlat;

    public MapperatorML(string modelPath, double lookBack = 2000, int numWindows = 4, int width = 128, int height = 96, float maxSdfDistance = 20) {
        this.lookBack = lookBack;
        this.numWindows = numWindows;
        this.maxSdfDistance = maxSdfDistance;
        this.width = width;
        this.height = height;
        decoder = new HitObjectDecoder();

        model = Models.GetModel2(new Shape(height, width, numWindows));
        model.load_weights(modelPath);

        flatNum = width * height;
        var x = math_ops.linspace(tf.constant(0d), tf.constant(512d), width);
        var y = math_ops.linspace(tf.constant(0d), tf.constant(384d), height);
        var XY = array_ops.meshgrid(new[] { x, y });
        coordinates = tf.expand_dims(tf.stack(XY, 2), 2);
        coordinatesFlat = tf.reshape(coordinates, new Shape(flatNum, 2));
    }

    /// <summary>
    /// Mapperates all of the pattern onto the list of hit objects.
    /// </summary>
    public void MapPattern(ReadOnlyMemory<MapDataPoint> pattern, IList<HitObject> hitObjects, float hitObjectRadius, Timing? timing = null, List<ControlChange>? controlChanges = null) {
        (var pos, double _, double time) = new Continuation(hitObjects);

        var hitObjectData = new List<(double, Tensor)>(hitObjects.Where(o => o is not Spinner).Select(GetData));

        for (var i = 0; i < pattern.Length; i++) {
            var dataPoint = pattern.Span[i];
            var originalHo = string.IsNullOrWhiteSpace(dataPoint.HitObject) ? null : decoder.Decode(dataPoint.HitObject);

            // Predict new position using AI
            var input = tf.expand_dims(GetMultiSdf(hitObjectData, hitObjectRadius, time), 0);
            var output = model.predict(input);
            var probsByDist = DistanceProbability(ToArray(pos), Math.Max(dataPoint.Spacing, 1));
            pos = WeightedRandomPosition(probsByDist * output);
            time = timing?.WalkBeatsInMillisecondTime(dataPoint.BeatsSince, time) ?? time + 1;

            Console.WriteLine($"time = {time}, pos = {pos}");

            if (dataPoint.DataType == Model.DataType.Release && hitObjects.Count > 0) {
                if (hitObjects[^1] is Spinner lastSpinner) {
                    // Make sure the last object ends at time t
                    lastSpinner.SetEndTime(time);
                }
                else {
                    // Make sure the last object is a slider of the release datapoint
                    var lastHitObject = hitObjects[^1];
                    hitObjects.RemoveAt(hitObjects.Count - 1);
                    if (lastHitObject is not Spinner) hitObjectData.RemoveAt(hitObjectData.Count - 1);

                    var lastSlider = new Slider {
                        Pos = lastHitObject.Pos,
                        StartTime = lastHitObject.StartTime,
                        SliderType = PathType.Linear,
                        PixelLength = Vector2.Distance(lastHitObject.Pos, pos),
                        CurvePoints = { pos }
                    };

                    if (dataPoint.Repeats.HasValue) {
                        lastSlider.RepeatCount = dataPoint.Repeats.Value;
                    }

                    if (originalHo is not null) {
                        lastSlider.ResetHitsounds();
                        lastSlider.Hitsounds = originalHo.Hitsounds;
                        if (originalHo is Slider slider2) {
                            lastSlider.EdgeHitsounds = slider2.EdgeHitsounds;
                        }
                    }

                    lastSlider.NewCombo = lastHitObject.NewCombo;
                    lastSlider.SetContext(new TimingContext(timing?.GlobalSliderMultiplier ?? 1.4, 1, new TimingPoint(), new TimingPoint(), new TimingPoint()));
                    lastSlider.SetEndTimeBySliderVelocity(time);
                    hitObjects.Add(lastSlider);
                    hitObjectData.Add(GetData(lastSlider));

                    // Make sure the last object ends at time t and around pos
                    // Rotate and scale the end towards the release pos
                    lastSlider.RecalculateEndPosition();
                    var ogPos = lastSlider.Pos;
                    double ogTheta = (lastSlider.EndPos - ogPos).Theta;
                    double newTheta = (pos - ogPos).Theta;
                    double ogSize = (lastSlider.EndPos - ogPos).Length;
                    double newSize = (pos - ogPos).Length;
                    double scale = newSize / ogSize;

                    if (!double.IsNaN(ogTheta) && !double.IsNaN(newTheta)) {
                        lastSlider.Transform(Matrix2.CreateRotation(ogTheta - newTheta));
                        lastSlider.Transform(Matrix2.CreateScale(scale));
                        lastSlider.Move(ogPos - lastSlider.Pos);
                        lastSlider.PixelLength *= scale;
                    }

                    // Add the right number of repeats
                    if (dataPoint.Repeats.HasValue) {
                        lastSlider.RepeatCount = dataPoint.Repeats.Value;
                    }

                    if (timing is not null && controlChanges is not null) {
                        // Adjust SV
                        var tp = timing.GetTimingPointAtTime(lastSlider.StartTime).Copy();
                        double mpb = timing.GetMpBAtTime(lastSlider.StartTime);
                        tp.Offset = lastSlider.StartTime;
                        tp.Uninherited = false;
                        tp.SetSliderVelocity(lastSlider.PixelLength / ((time - lastSlider.StartTime) / mpb *
                                                                       100 * timing.GlobalSliderMultiplier));
                        controlChanges.Add(new ControlChange(tp, true));
                    }
                }
            }

            if (dataPoint.DataType == Model.DataType.Release) continue;

            // Add hitobject on hit
            HitObject ho = dataPoint.DataType switch {
                Model.DataType.Spin => new Spinner(),
                Model.DataType.Hit => new HitCircle(),
                Model.DataType.Release => new HitCircle(),
                _ => throw new ArgumentOutOfRangeException(nameof(pattern), dataPoint.DataType, null)
            };

            ho.StartTime = time;
            ho.NewCombo = dataPoint.NewCombo;
            ho.Move(pos - ho.Pos);
            ho.ResetHitsounds();

            if (originalHo is not null) {
                ho.Hitsounds = originalHo.Hitsounds;
            }

            hitObjects.Add(ho);
            hitObjectData.Add(GetData(ho));
        }
    }

    private Vector2 WeightedRandomPosition(Tensor weights) {
        var probsBatch = tf.reshape(weights, new Shape(1, -1));
        var logProbBatch = tf.log(probsBatch).numpy();
        var indices = tf.random.categorical(logProbBatch, 1, output_dtype: dtypes.int32).numpy();
        var posIndexTensor = np.reshape(indices, new Shape(Array.Empty<int>()));
        var posIndex = (int)posIndexTensor;
        double[] posArray = coordinatesFlat[posIndex].ToArray<double>();
        return new Vector2(posArray[0], posArray[1]);
    }

    private static (double, Tensor) GetData(HitObject ho) {
        if (ho is not Slider s) return (ho.StartTime, tf.expand_dims(ToArray(ho.Pos), 0));

        var sliderPath = s.GetSliderPath();
        const int numPoints = 50;
        var arr = tf.stack(Enumerable.Range(0, numPoints + 1).Select(t => ToArray(sliderPath.PositionAt((double)t / numPoints))).ToArray());
        return (ho.EndTime, arr);
    }

    private static Tensor ToArray(Vector2 vec) {
        return tf.constant(new[] { vec.X, vec.Y }, dtypes.float32);
    }

    private Tensor GetMultiSdf(IList<(double, Tensor)> hitObjectData, float hitObjectRadius, double time) {
        double windowSize = lookBack / numWindows;
        var sdfs = new Tensor[numWindows];

        for (var i = 0; i < numWindows; i++) {
            double start = time - (i + 1) * windowSize;
            double end = time - i * windowSize + Precision.DOUBLE_EPSILON;

            var geometryInWindow = GetHitObjectDataInWindow(hitObjectData, start, end).ToArray();
            var data = geometryInWindow.Length > 0 ? tf.concat(geometryInWindow, 0) : tf.zeros(new Shape(0, 2), dtypes.float32);
            sdfs[i] = GeometryToSdf(data, hitObjectRadius);
        }

        return tf.stack(sdfs, 2);
    }

    private Tensor GeometryToSdf(Tensor geometry, double hitObjectRadius) {
        return tf.minimum(
            tf.sqrt(
                tf.min(
                    tf.sum(
                        tf.square(
                            tf.tile(
                                tf.reshape(
                                    geometry,
                                    (1, 1, -1, 2)
                                ),
                                (height, width, 1, 1)
                            ) - coordinates
                        ),
                        3
                    ),
                    2
                )
            ) / hitObjectRadius - 1,
            maxSdfDistance
        );
    }

    private Tensor GeometryToSdf2(Tensor geometry, double hitObjectRadius) {
        var sdf = new float[height, width];

        for (var i = 0; i < height; i++) {
            for (var j = 0; j < width; j++) {
                sdf[i, j] = (float)tf.minimum(
                    tf.sqrt(
                        tf.min(
                            tf.sum(
                                tf.square(
                                    geometry - coordinates[i, j]
                                ),
                                1
                            ),
                            0
                        )
                    ) / hitObjectRadius - 1,
                    maxSdfDistance
                );
            }
        }

        return tf.constant(sdf);
    }

    private Tensor DistanceProbability(Tensor pos, double distance) {
        return
            tf.exp(
            -tf.square(
                tf.sqrt(
                    tf.sum(
                        tf.square(
                            tf.tile(
                                tf.reshape(
                                    pos,
                                    (1, 2)
                                    ),
                                (flatNum, 1)
                                ) - coordinatesFlat
                            ),
                        1
                        )
                    ) / distance - 1
                ) * 4
            );
    }

    private static IEnumerable<Tensor> GetHitObjectDataInWindow(IEnumerable<(double, Tensor)> hitObjects, double start, double end) {
        return hitObjects.Where(o => o.Item1 > start && o.Item1 <= end).Select(o => o.Item2);
    }

    /// <summary>
    /// Mapperates all of the pattern onto the beatmap.
    /// </summary>
    public void MapPattern(ReadOnlyMemory<MapDataPoint> pattern, IBeatmap beatmap) {
        var controlChanges = new List<ControlChange>();
        MapPattern(pattern, beatmap.HitObjects, (float)beatmap.Difficulty.HitObjectRadius, beatmap.BeatmapTiming, controlChanges);
        ControlChange.ApplyChanges(beatmap.BeatmapTiming, controlChanges);
    }
}