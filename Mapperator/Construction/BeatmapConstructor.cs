using Mapperator.Matching;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper.Contexts;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools_Core.BeatmapHelper.IO.Decoding.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.ToolHelpers;

namespace Mapperator.Construction {
    public class BeatmapConstructor {
        private readonly HitObjectDecoder decoder;

        public bool SelectNewObjects { get; set; }

        public BeatmapConstructor() : this(new HitObjectDecoder()) { }

        public BeatmapConstructor(HitObjectDecoder decoder) {
            this.decoder = decoder;
        }

        /// <summary>
        /// Constructs the match onto the end of the list of hit objects.
        /// </summary>
        public Continuation Construct(IList<HitObject> hitObjects, Match match, ReadOnlySpan<MapDataPoint> input, Continuation? continuation = null, int? maxPlacements = null, Timing? timing = null, List<ControlChange>? controlChanges = null) {
            var (pos, angle, time) = continuation ?? new Continuation(hitObjects);

            var mult = match.MinMult == 0 && double.IsPositiveInfinity(match.MaxMult) ? 1 : Math.Sqrt(match.MinMult * match.MaxMult);
            var n = maxPlacements.HasValue ? Math.Min(match.Length, maxPlacements.Value) : match.Length;

            for (var i = 0; i < n; i++) {
                var dataPoint = match.Sequence.Span[i];

                var original = input[i];
                var originalHo = string.IsNullOrWhiteSpace(original.HitObject) ? null : decoder.Decode(original.HitObject);

                time = timing?.WalkBeatsInMillisecondTime(original.BeatsSince, time) ?? time + 1;
                angle += dataPoint.Angle;
                var dir = Vector2.Rotate(Vector2.UnitX, angle);
                pos += dataPoint.Spacing * mult * dir;
                // Wrap pos
                //pos = new Vector2(Helpers.Mod(pos.X, 512), Helpers.Mod(pos.Y, 384));
                pos = Vector2.Clamp(pos, Vector2.Zero, new Vector2(512, 382));

                //Console.WriteLine($"time = {time}, pos = {pos}, original = {original}, match = {match}");

                if (dataPoint.DataType == DataType.Release && hitObjects.Count > 0) {
                    if (hitObjects[^1] is Spinner lastSpinner) {
                        // Make sure the last object ends at time t
                        lastSpinner.SetEndTime(time);
                    }
                    else {
                        // Make sure the last object is a slider of the release datapoint
                        var lastHitObject = hitObjects[^1];
                        hitObjects.RemoveAt(hitObjects.Count - 1);

                        var ho = decoder.Decode(dataPoint.HitObject);
                        if (ho is Slider lastSlider) {
                            ho.StartTime = lastHitObject.StartTime;
                            ho.Move(lastHitObject.Pos - ho.Pos);
                        }
                        else {
                            lastSlider = new Slider {
                                Pos = lastHitObject.Pos,
                                StartTime = lastHitObject.StartTime,
                                SliderType = PathType.Linear,
                                PixelLength = Vector2.Distance(lastHitObject.Pos, pos),
                                CurvePoints = { pos }
                            };
                        }

                        if (original.Repeats.HasValue) {
                            lastSlider.RepeatCount = original.Repeats.Value;
                        }

                        if (originalHo is not null) {
                            lastSlider.ResetHitsounds();
                            lastSlider.Hitsounds = originalHo.Hitsounds;
                            if (originalHo is Slider slider2) {
                                lastSlider.EdgeHitsounds = slider2.EdgeHitsounds;
                            }
                        }

                        lastSlider.NewCombo = lastHitObject.NewCombo;
                        lastSlider.IsSelected = SelectNewObjects;
                        lastSlider.SetContext(new TimingContext(timing?.GlobalSliderMultiplier ?? 1.4, 1, new TimingPoint(), new TimingPoint(), new TimingPoint()));
                        lastSlider.SetEndTimeBySliderVelocity(time);
                        hitObjects.Add(lastSlider);

                        // Make sure the last object ends at time t and around pos
                        // Rotate and scale the end towards the release pos
                        lastSlider.RecalculateEndPosition();
                        var ogPos = lastSlider.Pos;
                        var ogTheta = (lastSlider.EndPos - ogPos).Theta;
                        var newTheta = (pos - ogPos).Theta;
                        var ogSize = (lastSlider.EndPos - ogPos).Length;
                        var newSize = (pos - ogPos).Length;
                        var scale = newSize / ogSize;

                        if (!double.IsNaN(ogTheta) && !double.IsNaN(newTheta)) {
                            lastSlider.Transform(Matrix2.CreateRotation(ogTheta - newTheta));
                            lastSlider.Transform(Matrix2.CreateScale(scale));
                            lastSlider.Move(ogPos - lastSlider.Pos);
                            lastSlider.PixelLength *= scale;
                        }

                        // Add the right number of repeats
                        if (original.Repeats.HasValue) {
                            lastSlider.RepeatCount = original.Repeats.Value;
                        }

                        if (timing is not null && controlChanges is not null) {
                            // Adjust SV
                            var tp = timing.GetTimingPointAtTime(lastSlider.StartTime).Copy();
                            var mpb = timing.GetMpBAtTime(lastSlider.StartTime);
                            tp.Offset = lastSlider.StartTime;
                            tp.Uninherited = false;
                            tp.SetSliderVelocity(lastSlider.PixelLength / ((time - lastSlider.StartTime) / mpb *
                                                                           100 * timing.GlobalSliderMultiplier));
                            controlChanges.Add(new ControlChange(tp, true));
                        }
                    }
                }

                // Add hitobject on hit
                if (dataPoint.DataType != DataType.Release) {
                    var ho = !string.IsNullOrEmpty(dataPoint.HitObject)
                        ? decoder.Decode(dataPoint.HitObject)
                        : null;

                    switch (dataPoint.DataType) {
                        case DataType.Spin when ho is not Spinner:
                            ho = new Spinner();
                            break;
                        case DataType.Hit when ho is not HitCircle:
                            ho = new HitCircle();
                            break;
                    }

                    ho!.IsSelected = SelectNewObjects;
                    ho.StartTime = time;
                    ho.NewCombo = original.NewCombo;
                    ho.Move(pos - ho.Pos);
                    ho.ResetHitsounds();

                    if (originalHo is not null) {
                        ho.Hitsounds = originalHo.Hitsounds;
                    }

                    hitObjects.Add(ho);
                }
            }

            return new Continuation(pos, angle, time);
        }
    }
}