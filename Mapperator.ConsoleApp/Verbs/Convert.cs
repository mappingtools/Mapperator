using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;
using Mapperator.ConsoleApp.Resources;
using Mapperator.Matching;
using Mapperator.Matching.DataStructures;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.ConsoleApp.Verbs;

public static class Convert {
    [Verb("convert", HelpText = "Reconstruct a beatmap using extracted beatmap data.")]
    public class ConvertOptions {
        [Option('d', "data", Required = true, HelpText = "Input extracted beatmap data for the conversion.")]
        public string? DataPath { get; [UsedImplicitly] set; }

        [Option('i', "input", Required = true, HelpText = "Input beatmap to be converted.")]
        public string? InputBeatmapPath { get; [UsedImplicitly] set; }

        [Option('o', "output", Required = true, HelpText = "Filename of the output.")]
        public string? OutputName { get; [UsedImplicitly] set; }

        [Option('g', "structInput", HelpText = "Serialized data structure file to speed up matching.")]
        public string? InputStructName { get; [UsedImplicitly] set; }

        [Option('h', "structOutput", HelpText = "Filename for the generated data structure.")]
        public string? OutputStructName { get; [UsedImplicitly] set; }

        [Option('s', "spacingMap", HelpText = "Filename a beatmap with the desired spacing distribution.")]
        public string? SpacingBeatmapPath { get; [UsedImplicitly] set; }

        [Option('v', "visualSpacing", HelpText = "Optimize visual spacing", Default = true)]
        public bool VisualSpacing { get; [UsedImplicitly] set; }

        [Option('a', "sliderAngles", HelpText = "Optimize slider angles", Default = true)]
        public bool SliderAngles { get; [UsedImplicitly] set; }
    }

    public static int DoMapConvert(ConvertOptions opts) {
        if (opts.DataPath is null) throw new ArgumentNullException(nameof(opts));

        // Start time measurement
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Console.WriteLine(Strings.Program_DoMapConvert_Extracting_data___);
        var trainData = DataSerializer.DeserializeBeatmapData(File.ReadLines(Path.ChangeExtension(opts.DataPath, ".txt")));
        var map = new BeatmapEditor(Path.ChangeExtension(opts.InputBeatmapPath, ".osu")).ReadFile();
        var input = new DataExtractor().ExtractBeatmapData(map).ToArray();

        // TODO: add options to automatically add distance spacing
        // TODO: also add options for ignoring angles, nc, or slider attributes
        // TODO: discourage extreme scaling values
        // TODO: prevent offscreen slider body
        // TODO: create some kind of procedural slider generation by splitting each slider segment into a separate data point
        // TODO: add post-processing filters for fixing overlaps, stacks, and slider angles
        // TODO: encourage better stream angles by removing pog bonus on NC
        // Change spacing distribution
        if (opts.SpacingBeatmapPath is not null) {
            Console.WriteLine(Strings.Program_DoMapConvert_Converting_spacing_to_reference_beatmap___);
            var spacingMap = new BeatmapEditor(Path.ChangeExtension(opts.SpacingBeatmapPath, ".osu")).ReadFile();
            var spacingMapData = new DataExtractor().ExtractBeatmapData(spacingMap).ToArray();
            input = TransferSpacing(spacingMapData, input);
        }

        // Add the data to the matcher or load the data
        Console.WriteLine(Strings.Program_DoMapConvert_Adding_data___);
        var data = new RhythmDistanceTrieStructure();
        if (data is ISerializable sData &&
            !string.IsNullOrEmpty(opts.InputStructName) &&
            File.Exists(Path.ChangeExtension(opts.InputStructName, sData.DefaultExtension))) {
            using Stream file = File.OpenRead(Path.ChangeExtension(opts.InputStructName, sData.DefaultExtension));
            sData.Load(trainData, file);
        } else {
            Stopwatch buildStopwatch = new Stopwatch();
            buildStopwatch.Start();

            foreach (var str in trainData) {
                data.Add(str.ToArray());
                Console.Write('.');
            }

            buildStopwatch.Stop();
            Console.WriteLine(Strings.Program_DoMapConvert_Elapsed_Time_is, buildStopwatch.ElapsedMilliseconds.ToString());

            if (data is ISerializable sData2 && !string.IsNullOrEmpty(opts.OutputStructName)) {
                using Stream file = File.Create(Path.ChangeExtension(opts.OutputStructName, sData2.DefaultExtension));
                sData2.Save(file);
            }
        }

        // Construct new beatmap
        Console.WriteLine(Strings.Program_DoMapConvert_Constructing_beatmap___);
        map.Metadata.Version = "Converted";
        map.HitObjects.Clear();
        map.Editor.Bookmarks.Clear();

        var mapperator = new Mapperator(data, input, map.Difficulty.ApproachTime + 500, map.Difficulty.HitObjectRadius,
            opts.VisualSpacing, opts.SliderAngles);
        mapperator.MapPattern(map);

        new BeatmapEditor(Path.ChangeExtension(opts.OutputName, ".osu")).WriteFile(map);

        // Print elapsed time
        stopwatch.Stop();
        Console.WriteLine(Strings.Program_DoMapConvert_Elapsed_Time_is, stopwatch.ElapsedMilliseconds.ToString());

        return 0;
    }

    private static MapDataPoint[] TransferSpacing(MapDataPoint[] from, MapDataPoint[] to) {
        var spacingData = new double[9][];
        var groupedByBeats = from.GroupBy(o => MathHelper.Clamp((int) Math.Round(Math.Log2(o.BeatsSince) + 6), 0, 8));
        foreach (var group in groupedByBeats) {
            var arr = group.OrderBy(o => o.Spacing).Select(o => o.Spacing).ToArray();
            spacingData[group.Key] = arr;
        }
        var sliderSpacingData = new double[9][];
        var sliderGroupedByBeats = from.Where(o => o.SliderLength.HasValue)
            .GroupBy(o => MathHelper.Clamp((int) Math.Round(Math.Log2(o.BeatsSince) + 6), 0, 8));
        foreach (var group in sliderGroupedByBeats) {
            var arr = group.OrderBy(o => o.SliderLength).Select(o => o.SliderLength!.Value).ToArray();
            sliderSpacingData[group.Key] = arr;
        }

        var inputSpacingData = to.Select(o => o.Spacing).ToList();
        inputSpacingData.Sort();

        return to.Select(o => new MapDataPoint(o.DataType, o.BeatsSince, Transform(o.Spacing, o.BeatsSince, spacingData), o.Angle,
            o.NewCombo, o.SliderType, o.SliderLength.HasValue ? Transform(o.SliderLength.Value, o.BeatsSince, sliderSpacingData) : null,
            o.SliderSegments, o.Repeats, o.HitObject)).ToArray();

        double Transform(double spacing, double beats, double[]?[] data) {
            var gap = MathHelper.Clamp((int)Math.Round(Math.Log2(beats) + 6), 0, 8);
            var index = inputSpacingData.IndexOf(spacing);
            if (data[gap] is null || data[gap]!.Length == 0)
                return 0;
            return data[gap]![MathHelper.Clamp((int)Math.Round((double)index / inputSpacingData.Count * data[gap]!.Length), 0, data[gap]!.Length - 1)];
        }
    }
}