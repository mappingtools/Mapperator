using System;
using System.IO;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;
using Mapperator.ConsoleApp.Resources;
using Mapperator.Matching;
using Mapperator.Matching.DataStructures;

namespace Mapperator.ConsoleApp.Verbs;

public static class Build {
    [Verb("build", HelpText = "Build a data structure using extracted beatmap data.")]
    public class BuildOptions {
        [Option('d', "data", Required = true, HelpText = "Input extracted beatmap data for the graph.")]
        public string? DataPath { get; [UsedImplicitly] set; }

        [Option('h', "structOutput", Required = true, HelpText = "Filename for the generated data structure.")]
        public string? OutputStructName { get; [UsedImplicitly] set; }
    }

    public static int DoBuildGraph(BuildOptions opts) {
        if (opts.OutputStructName is null) throw new ArgumentNullException(nameof(opts));
        if (opts.DataPath is null) throw new ArgumentNullException(nameof(opts));

        var trainData = DataSerializer.DeserializeBeatmapData(File.ReadLines(Path.ChangeExtension(opts.DataPath, ".txt")));
        var data = new RhythmDistanceTrieStructure();

        if (data is not ISerializable sData) {
            Console.WriteLine(Strings.Program_DoBuildGraph_The__0__matcher_is_not_compatible_with_building_);
            return 0;
        }

        foreach (var str in trainData) {
            data.Add(str.ToArray());
        }

        using Stream file = File.Create(Path.ChangeExtension(opts.OutputStructName, sData.DefaultExtension));
        sData.Save(file);

        return 0;
    }
}