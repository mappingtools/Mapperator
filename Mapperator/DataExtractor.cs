using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools_Core.BeatmapHelper.IO.Encoding.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator {
    public class DataExtractor {
        private readonly HitObjectEncoder encoder;
        private readonly int dataVersion;

        public DataExtractor(int dataVersion = DataSerializer.CurrentDataVersion) : this(new HitObjectEncoder(), dataVersion) { }

        public DataExtractor(HitObjectEncoder encoder, int dataVersion) {
            this.encoder = encoder;
            this.dataVersion = dataVersion;
        }

        public IEnumerable<MapDataPoint> ExtractBeatmapData(IBeatmap beatmap, bool mirror = false) {
            return ExtractBeatmapData(beatmap.HitObjects, beatmap.BeatmapTiming, mirror);
        }

        public IEnumerable<MapDataPoint> ExtractBeatmapData(IEnumerable<HitObject> hitobjects, Timing timing, bool mirror = false) {
            var lastPos = new Vector2(256, 192);  // Playfield centre
            var lastLastPos = new Vector2(0, 192);  // Playfield left-centre
            double lastTime = 0;
            foreach (var ho in hitobjects) {
                switch (ho) {
                    case HitCircle:
                        yield return CreateDataPoint(timing, ho.Pos, ho.StartTime, DataType.Hit, null, null, ho.NewCombo, ho, ref lastLastPos, ref lastPos, ref lastTime, mirror);
                        break;
                    case Slider slider:
                        yield return CreateDataPoint(timing, ho.Pos, ho.StartTime, DataType.Hit, null, null, ho.NewCombo, ho, ref lastLastPos, ref lastPos, ref lastTime, mirror);

                        // Get middle and end positions too for every repeat
                        var path = slider.GetSliderPath();
                        //var startPos = slider.Pos;
                        //var middlePos = path.PositionAt(0.5);
                        var endPos = path.PositionAt(1);

                        // Count the number of segments in the slider
                        var segments = 0;
                        var controlPoints = path.ControlPoints;

                        if (dataVersion >= 2 && slider.SliderType == PathType.Linear) {
                            for (var i = 0; i < controlPoints.Count - 1; i++) {
                                if (controlPoints[i] != controlPoints[i + 1]) {
                                    segments++;
                                }
                            }
                        } else {
                            for (var i = 0; i < controlPoints.Count; i++) {
                                if (i == controlPoints.Count - 1 || controlPoints[i] == controlPoints[i + 1] && i != controlPoints.Count - 2) {
                                    segments++;
                                }
                            }
                        }

                        yield return CreateDataPoint(timing, endPos, slider.StartTime + slider.SpanDuration, DataType.Release, slider.SliderType, slider.RepeatCount, false, ho, ref lastLastPos, ref lastPos, ref lastTime, mirror, slider.PixelLength, segments);

                        //for (int i = 0; i < slider.RepeatCount + 1; i++) {
                        //    yield return CreateDataPoint(timing, middlePos, slider.StartTime + slider.SpanDuration * (i + 0.5), DataType.Hold, slider.SliderType, null, ref lastLastPos, ref lastPos, ref lastTime, reverseRotation);
                        //    yield return CreateDataPoint(timing, i % 2 == 0 ? endPos : startPos, slider.StartTime + slider.SpanDuration * (i + 1), i == slider.RepeatCount ? DataType.Release : DataType.Hold, slider.SliderType, null, ref lastLastPos, ref lastPos, ref lastTime, reverseRotation);
                        //}
                        break;
                    case Spinner spinner:
                        yield return CreateDataPoint(timing, ho.Pos, ho.StartTime, DataType.Spin, null, null, true, ho,
                            ref lastLastPos, ref lastPos, ref lastTime, mirror);
                        yield return CreateDataPoint(timing, ho.Pos, spinner.EndTime, DataType.Release, null, 0, false, ho,
                            ref lastLastPos, ref lastPos, ref lastTime, mirror);
                        break;
                }
            }
        }

        private MapDataPoint CreateDataPoint(Timing timing, Vector2 pos, double time, DataType dataType, PathType? sliderType, int? repeats, bool nc, HitObject? hitObject, ref Vector2 lastLastPos, ref Vector2 lastPos, ref double lastTime, bool mirror = false, double? sliderLength = null, int? sliderSegments = null) {
            //var angle = Vector2.Angle(pos - lastPos, lastPos - lastLastPos);
            var angle = Helpers.AngleDifference((lastPos - lastLastPos).Theta, (pos - lastPos).Theta);
            if (double.IsNaN(angle)) {
                angle = 0;
            }

            var ho = hitObject;
            if (mirror && hitObject is not null) {
                // Mirror the hit object 
                ho = hitObject.DeepClone();
                ho.Transform(new Matrix2(1, 0, 0, -1));
                ho.Move(new Vector2(0, 384));
            }
            var hoString = hitObject is null ? string.Empty : encoder.Encode(ho);

            var point = new MapDataPoint(
                                dataType,
                                timing.GetBeatLength(lastTime, time),
                                Vector2.Distance(pos, lastPos),
                                mirror ? -angle : angle,
                                nc,
                                sliderType,
                                sliderLength,
                                sliderSegments,
                                repeats,
                                hoString
                                );

            lastLastPos = lastPos;
            lastPos = pos;
            lastTime = time;

            return point;
        }
    }
}
