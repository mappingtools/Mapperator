using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Contexts;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools_Core.BeatmapHelper.IO.Encoding.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator {
    public class DataExtractor2 {
        public IEnumerable<MapDataPoint2> ExtractBeatmapData(IBeatmap beatmap, bool mirror = false) {
            return ExtractBeatmapData(beatmap.HitObjects, beatmap.BeatmapTiming, beatmap.Difficulty.SliderTickRate, mirror);
        }

        public IEnumerable<MapDataPoint2> ExtractBeatmapData(IEnumerable<HitObject> hitobjects, Timing timing, double sliderTickRate, bool mirror = false) {
            var lastPos = new Vector2(256, 192);  // Playfield centre
            var lastLastPos = new Vector2(0, 192);  // Playfield left-centre
            int lastTime = 0;
            foreach (var ho in hitobjects) {
                switch (ho) {
                    case HitCircle:
                        yield return CreateDataPoint(ho.Pos, (int)ho.StartTime, DataType2.HitCircle, ref lastLastPos, ref lastPos, ref lastTime, mirror);
                        break;
                    case Slider slider:
                        yield return CreateDataPoint(ho.Pos, (int)ho.StartTime, DataType2.SliderStart, ref lastLastPos, ref lastPos, ref lastTime, mirror);

                        // Get middle and end positions too for every repeat
                        var path = slider.GetSliderPath();
                        var startPos = slider.Pos;
                        //var middlePos = path.PositionAt(0.5);

                        if (!ho.TryGetContext<TimingContext>(out var timing2)) {
                            throw new InvalidOperationException("Slider is not initialized with timing context. Can not get the slider ticks.");
                        }

                        var t = timing2.UninheritedTimingPoint.MpB / sliderTickRate;
                        var tick_ts = new List<double>();
                        while (t + 10 < slider.SpanDuration) {
                            var t2 = t / slider.SpanDuration;
                            tick_ts.Add(t2);

                            t += timing2.UninheritedTimingPoint.MpB / sliderTickRate;
                        }

                        var endPos = path.PositionAt(1);

                        for (int i = 0; i < slider.SpanCount; i++) {
                            // Do ticks
                            for (int j = 0; j < tick_ts.Count; j++) {
                                var k = i % 2 == 0 ? j : tick_ts.Count - j - 1;
                                var t2 = tick_ts[k];
                                var pos = path.PositionAt(t2);
                                var time = (int)(slider.StartTime + i * slider.SpanDuration + (i % 2 == 0 ? t2 : 1 - t2) * slider.SpanDuration);

                                yield return CreateDataPoint(pos, time, DataType2.SliderTick, ref lastLastPos, ref lastPos, ref lastTime, mirror);
                            }

                            // Do end
                            yield return CreateDataPoint(i % 2 == 0 ? endPos : startPos, (int)(slider.StartTime + slider.SpanDuration * (i + 1)), i == slider.RepeatCount ? DataType2.SliderEnd : DataType2.SliderRepeat, ref lastLastPos, ref lastPos, ref lastTime, mirror);
                        }

                        break;
                    case Spinner spinner:
                        yield return CreateDataPoint(ho.Pos, (int)ho.StartTime, DataType2.SpinStart, ref lastLastPos, ref lastPos, ref lastTime, mirror);
                        yield return CreateDataPoint(ho.Pos, (int)spinner.EndTime, DataType2.SpinEnd, ref lastLastPos, ref lastPos, ref lastTime, mirror);
                        break;
                }
            }
        }

        private MapDataPoint2 CreateDataPoint(Vector2 pos, int time, DataType2 dataType, ref Vector2 lastLastPos, ref Vector2 lastPos, ref int lastTime, bool mirror = false) {
            //var angle = Vector2.Angle(pos - lastPos, lastPos - lastLastPos);
            var angle = Helpers.AngleDifference((lastPos - lastLastPos).Theta, (pos - lastPos).Theta);
            if (double.IsNaN(angle)) {
                angle = 0;
            }

            var point = new MapDataPoint2(
                                dataType,
                                time - lastTime,
                                Vector2.Distance(pos, lastPos),
                                mirror ? -angle : angle
                                );

            lastLastPos = lastPos;
            lastPos = pos;
            lastTime = time;

            return point;
        }
    }
}
