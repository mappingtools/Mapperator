using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools_Core.BeatmapHelper.IO.Encoding.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;
using Mapping_Tools_Core.MathUtil;
using System.Collections.Generic;

namespace Mapperator {
    public static class DataExtractor {
        public static IEnumerable<MapDataPoint> ExtractBeatmapData(IBeatmap beatmap) {
            Vector2 lastPos = new Vector2(256, 196);  // Playfield centre
            Vector2 lastLastPos = new Vector2(0, 196);  // Playfield left-centre
            double lastTime = 0;
            var timing = beatmap.BeatmapTiming;
            var encoder = new HitObjectEncoder();
            foreach (var ho in beatmap.HitObjects) {
                switch (ho) {
                    case HitCircle:
                        yield return CreateDataPoint(timing, ho.Pos, ho.StartTime, DataType.Hit, null, encoder.Encode(ho), ref lastLastPos, ref lastPos, ref lastTime);
                        break;
                    case Slider slider:
                        yield return CreateDataPoint(timing, ho.Pos, ho.StartTime, DataType.Hit, slider.SliderType, encoder.Encode(ho), ref lastLastPos, ref lastPos, ref lastTime);

                        // Get middle and end positions too for every repeat
                        var path = slider.GetSliderPath();
                        var startPos = slider.Pos;
                        var middlePos = path.PositionAt(0.5);
                        var endPos = path.PositionAt(1);

                        for (int i = 0; i < slider.RepeatCount + 1; i++) {
                            yield return CreateDataPoint(timing, middlePos, slider.StartTime + slider.SpanDuration * (i + 0.5), DataType.Hold, slider.SliderType, null, ref lastLastPos, ref lastPos, ref lastTime);
                            yield return CreateDataPoint(timing, i % 2 == 0 ? endPos : startPos, slider.StartTime + slider.SpanDuration * (i + 1), i == slider.RepeatCount ? DataType.Release : DataType.Hold, slider.SliderType, null, ref lastLastPos, ref lastPos, ref lastTime);
                        }
                        break;
                }
            }
        }

        private static MapDataPoint CreateDataPoint(Timing timing, Vector2 pos, double time, DataType dataType, PathType? sliderType, string hitObject, ref Vector2 lastLastPos, ref Vector2 lastPos, ref double lastTime) {
            var angle = Vector2.Angle(pos - lastPos, lastPos - lastLastPos);
            if (double.IsNaN(angle)) {
                angle = 0;
            }

            var point = new MapDataPoint(
                                dataType,
                                timing.GetBeatLength(lastTime, time),
                                Vector2.Distance(pos, lastPos),
                                angle,
                                sliderType,
                                hitObject
                                );

            lastLastPos = lastPos;
            lastPos = pos;
            lastTime = time;

            return point;
        }
    }
}
