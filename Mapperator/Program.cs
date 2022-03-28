using CommandLine;
using HNSW.Net;
using Mapperator.Model;
using Mapperator.Resources;
using Mapping_Tools_Core;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Contexts;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools_Core.BeatmapHelper.IO.Decoding.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.ToolHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mapperator {
    public class Program {
        [Verb("extract", HelpText = "Extract beatmap data from an osu! collection.")]
        class ExtractOptions {
            [Option('c', "collection", Group = "input", HelpText = "Name of osu! collection to be extracted.")]
            public string CollectionName { get; set; }

            [Option('i', "input", Group = "input", HelpText = "Input beatmaps to be extracted.")]
            public IEnumerable<string> InputFiles { get; set; }

            [Option('o', "output", Required = true, HelpText = "Filename of the output.")]
            public string OutputName { get; set; }
        }

        [Verb("build", HelpText = "Build a HNSW graph using extracted beatmap data.")]
        class BuildOptions {
            [Option('d', "data", Required = true, HelpText = "Input extracted beatmap data for the graph.")]
            public string DataPath { get; set; }

            [Option('h', "graphOutput", Required = true, HelpText = "Filename for the generated HNSW graph data.")]
            public string OutputGraphName { get; set; }
        }

        [Verb("convert", HelpText = "Reconstruct a beatmap using extracted beatmap data.")]
        class ConvertOptions {
            [Option('d', "data", Required = true, HelpText = "Input extracted beatmap data for the conversion.")]
            public string DataPath { get; set; }

            [Option('i', "input", Required = true, HelpText = "Input beatmap to be converted.")]
            public string InputBeatmapPath { get; set; }

            [Option('o', "output", Required = true, HelpText = "Filename of the output.")]
            public string OutputName { get; set; }

            [Option('g', "graph", HelpText = "HNSW graph data file to speed up conversion.")]
            public string GraphPath { get; set; }

            [Option('h', "graphOutput", HelpText = "Filename for the generated HNSW graph data.")]
            public string OutputGraphName { get; set; }
        }

        [Verb("search", HelpText = "Search your entire Songs folder for a specific pattern.")]
        class SearchOptions {
            [Option('p', "pattern", Required = true, HelpText = "Prints all messages to standard output.")]
            public string Pattern { get; set; }

            [Option('c', "collection", HelpText = "Name of osu! collection to be searched.")]
            public string CollectionName { get; set; }
        }

        static int Main(string[] args) {
            ConfigManager.LoadConfig();

            return Parser.Default.ParseArguments<ExtractOptions, BuildOptions, ConvertOptions, SearchOptions>(args)
              .MapResult(
                (ExtractOptions opts) => DoDataExtraction(opts),
                (BuildOptions opts) => DoBuildGraph(opts),
                (ConvertOptions opts) => DoMapConvert(opts),
                (SearchOptions opts) => DoPatternSearch(opts),
                errs => 1);
        }

        private static int DoPatternSearch(SearchOptions opts) {
            int matches = 0;
            int i = 0;
            foreach (var path in string.IsNullOrEmpty(opts.CollectionName) ? Directory.EnumerateFiles(ConfigManager.Config.SongsPath, "*.osu",
                new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true, ReturnSpecialDirectories = false }) :
                DbManager.GetCollection(opts.CollectionName).Select(o => Path.Combine(ConfigManager.Config.SongsPath, o.FolderName, o.FileName))) {
                PatternSearchMap(path, opts.Pattern, i++, ref matches);
            }
            return 0;
        }

        private static bool PatternSearchMap(string path, string pattern, int i, ref int matches) {
            if (i % 1000 == 0) {
                Console.Write('.');
            }
            //Console.WriteLine(path);

            var startBracketIndex = pattern.IndexOf("(", StringComparison.Ordinal);
            var endBracketIndex = pattern.IndexOf(")", StringComparison.Ordinal);
            double t = InputParsers.ParseOsuTimestamp(pattern).TotalMilliseconds;
            int l = 0;
            if (startBracketIndex != -1) {
                if (endBracketIndex == -1) {
                    endBracketIndex = pattern.Length - 1;
                }

                // Get the part of the code between the brackets
                var comboNumbersString = pattern.Substring(startBracketIndex + 1, endBracketIndex - startBracketIndex - 1);

                l = comboNumbersString.Split(',').Length;
            }

            try {
                var beatmap = new BeatmapEditor(path).ReadFile();
                var en = beatmap.QueryTimeCode(pattern);
                var hos = en.ToArray();
                if (hos.Length == l && Precision.AlmostEquals(hos[0].StartTime, t)) {
                    matches++;
                    Console.WriteLine($"Found match {matches} in beatmap: {path}");
                    return true;
                }
            } catch (Exception e) {
                Console.WriteLine("Can't parse this map: " + path);
                Console.WriteLine(e);
            }
            return false;
        }

        private static int DoBuildGraph(BuildOptions opts) {
            var trainData = DataSerializer.DeserializeBeatmapData(File.ReadLines(Path.ChangeExtension(opts.DataPath, ".txt"))).ToList();
            var matcher = new DataMatcher();
            var graph = matcher.CreateGraph(trainData);
            matcher.SaveGraph(graph, Path.ChangeExtension(opts.OutputGraphName, ".hnsw"));
            return 0;
        }

        private static int DoMapConvert(ConvertOptions opts) {
            Console.WriteLine("Extracting data...");
            var trainData = DataSerializer.DeserializeBeatmapData(File.ReadLines(Path.ChangeExtension(opts.DataPath, ".txt"))).ToList();
            var map = new BeatmapEditor(Path.ChangeExtension(opts.InputBeatmapPath, ".osu")).ReadFile();
            var input = new DataExtractor().ExtractBeatmapData(map).ToList();

            var matcher = new DataMatcher();
            SmallWorld<MapDataPoint[], double> graph;
            if (!string.IsNullOrEmpty(opts.GraphPath) && File.Exists(Path.ChangeExtension(opts.GraphPath, ".hnsw"))) {
                graph = matcher.LoadGraph(trainData, Path.ChangeExtension(opts.GraphPath, ".hnsw"));
            } else {
                graph = matcher.CreateGraph(trainData);
                if (!string.IsNullOrEmpty(opts.OutputGraphName))
                    matcher.SaveGraph(graph, Path.ChangeExtension(opts.OutputGraphName, ".hnsw"));
            }

            // Construct new beatmap
            Console.WriteLine("Constructing beatmap...");
            var decoder = new HitObjectDecoder();
            int i = 0;
            double time = 0;
            Vector2 pos = map.HitObjects[0].Pos;
            double angle = 0;
            MapDataPoint? lastMatch = null;
            var controlChanges = new List<ControlChange>();
            map.Metadata.Version = "Converted";
            map.HitObjects.Clear();
            map.Editor.Bookmarks.Clear();
            foreach (var match in matcher.FindSimilarData2(graph, input, m => IsInBounds(m, pos, angle, decoder))) {
                var original = input[i++];
                var originalHo = string.IsNullOrWhiteSpace(original.HitObject) ? null : decoder.Decode(original.HitObject);

                time += map.BeatmapTiming.GetMpBAtTime(time) * original.BeatsSince;
                angle += match.Angle;
                var dir = Vector2.Rotate(Vector2.UnitX, angle);
                pos += match.Spacing * dir;
                // Wrap pos
                //pos = new Vector2(Helpers.Mod(pos.X, 512), Helpers.Mod(pos.Y, 384));
                pos = Vector2.Clamp(pos, Vector2.Zero, new Vector2(512, 382));

                if (match.DataType == DataType.Release) {
                    map.Editor.Bookmarks.Add(time);
                    // Make sure the last object is a slider
                    if (map.HitObjects.LastOrDefault() is HitCircle lastCircle) {
                        map.HitObjects.RemoveAt(map.HitObjects.Count - 1);
                        var slider = new Slider {
                            Pos = lastCircle.Pos,
                            StartTime = lastCircle.StartTime,
                            SliderType = PathType.Linear,
                            PixelLength = Vector2.Distance(lastCircle.Pos, pos),
                            CurvePoints = { pos }
                        };
                        map.HitObjects.Add(slider);
                    }
                    // Make sure the last object ends at time t and around pos
                    if (map.HitObjects.LastOrDefault() is Slider lastSlider) {
                        // Adjust SV
                        var tp = map.BeatmapTiming.GetTimingPointAtTime(lastSlider.StartTime).Copy();
                        var mpb = map.BeatmapTiming.GetMpBAtTime(lastSlider.StartTime);
                        tp.Offset = lastSlider.StartTime;
                        tp.Uninherited = false;
                        tp.SetSliderVelocity(lastSlider.PixelLength / ((time - lastSlider.StartTime) / mpb * 100 * map.Difficulty.SliderMultiplier));
                        controlChanges.Add(new ControlChange(tp, true));
                    } 
                }

                if (match.DataType == DataType.Hit) {
                    // If the last object is a slider and there is no release previously, then make sure the object is a circle
                    if (lastMatch.HasValue && lastMatch.Value.DataType == DataType.Hit && map.HitObjects.LastOrDefault() is Slider lastSlider) {
                        map.HitObjects.RemoveAt(map.HitObjects.Count - 1);
                        map.HitObjects.Add(new HitCircle { Pos = lastSlider.Pos, StartTime = lastSlider.StartTime });
                    }
                }

                if (!string.IsNullOrEmpty(match.HitObject)) {
                    var ho = decoder.Decode(match.HitObject);
                    if (ho is Slider slider) {
                        slider.RepeatCount = 0;
                        slider.Transform(Matrix2.CreateRotation(angle));
                        if (originalHo is Slider oSlider) {
                            slider.RepeatCount = oSlider.RepeatCount;
                        }
                    }
                    ho.StartTime = time;
                    ho.Move(pos - ho.Pos);
                    ho.ResetHitsounds();
                    if (originalHo is not null) {
                        ho.Hitsounds = originalHo.Hitsounds;
                    }
                    map.HitObjects.Add(ho);
                }

                lastMatch = match;
            }
            ControlChange.ApplyChanges(map.BeatmapTiming, controlChanges);
            new BeatmapEditor(Path.ChangeExtension(opts.OutputName, ".osu")).WriteFile(map);
            return 0;
        }

        private static bool IsInBounds(MapDataPoint match, Vector2 pos, double angle, HitObjectDecoder decoder) {
            angle += match.Angle;
            var dir = Vector2.Rotate(Vector2.UnitX, angle);
            pos += match.Spacing * dir;

            if (!PosInBounds(pos)) {
                return false;
            } 

            if (!string.IsNullOrEmpty(match.HitObject)) {
                var ho = decoder.Decode(match.HitObject);
                if (ho is Slider slider) {
                    slider.Transform(Matrix2.CreateRotation(angle));
                    ho.Move(pos - ho.Pos);
                    slider.RecalculateEndPosition();
                    if (!PosInBounds(slider.EndPos)) {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool PosInBounds(Vector2 pos) {
            return pos.X >= -5 && pos.X <= 517 && pos.Y >= -5 && pos.Y <= 387;
        }

        static int DoDataExtraction(ExtractOptions opts) {
            var extractor = new DataExtractor();
            File.WriteAllLines(Path.ChangeExtension(opts.OutputName, ".txt"),
                (string.IsNullOrEmpty(opts.CollectionName) ? opts.InputFiles :
                DbManager.GetCollection(opts.CollectionName).Select(o => Path.Combine(ConfigManager.Config.SongsPath, o.FolderName, o.FileName)))
                .Select(o => new BeatmapEditor(o).ReadFile())
                .SelectMany(b => extractor.ExtractBeatmapData(b).Concat(extractor.ExtractBeatmapData(b, true)))
                .Select(DataSerializer.SerializeBeatmapDataSample));
            return 0;
        }

        static bool CheckLength(string[] args, int neededLength) {
            if (args.Length < neededLength) {
                Console.WriteLine(string.Format(Strings.NotEnoughArguments, neededLength));
                return false;
            }
            return true;
        }
    }
}
