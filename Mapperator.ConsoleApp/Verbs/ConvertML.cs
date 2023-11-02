using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;
using Mapperator.ConsoleApp.Resources;
using Mapperator.ML;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;

namespace Mapperator.ConsoleApp.Verbs;

public static class ConvertML {
    [Verb("convert-ml", HelpText = "Reconstruct a beatmap using machine learning.")]
    public class ConvertMLOptions {
        [Option('m', "model", Required = true, HelpText = "Path to the saved ML model.")]
        public string? ModelPath { get; [UsedImplicitly] set; }

        [Option('i', "input", Required = true, HelpText = "Input beatmap to be converted.")]
        public string? InputBeatmapPath { get; [UsedImplicitly] set; }

        [Option('o', "output", Required = true, HelpText = "Filename of the output.")]
        public string? OutputName { get; [UsedImplicitly] set; }
    }

    public static int DoMapConvert(ConvertMLOptions opts) {
        if (opts.ModelPath is null) throw new ArgumentNullException(nameof(opts));

        // Start time measurement
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        Console.WriteLine(Strings.Program_DoMapConvert_Extracting_data___);
        var map = new BeatmapEditor(Path.ChangeExtension(opts.InputBeatmapPath, ".osu")).ReadFile();
        var input = new DataExtractor().ExtractBeatmapData(map).ToArray();

        Console.WriteLine(Strings.ConvertML_DoMapConvert_Loading_ML_model___);
        var mapperator = new MapperatorML(opts.ModelPath);

        // Construct new beatmap
        Console.WriteLine(Strings.Program_DoMapConvert_Constructing_beatmap___);
        map.Metadata.Version = "AI Converted";
        map.HitObjects.Clear();
        map.Editor.Bookmarks.Clear();

        mapperator.MapPattern(input, map);

        new BeatmapEditor(Path.ChangeExtension(opts.OutputName, ".osu")).WriteFile(map);

        // Print elapsed time
        stopwatch.Stop();
        Console.WriteLine(Strings.Program_DoMapConvert_Elapsed_Time_is, stopwatch.ElapsedMilliseconds.ToString());

        return 0;
    }
}