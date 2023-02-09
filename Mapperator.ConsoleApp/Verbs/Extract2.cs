using System;
using System.IO;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;

namespace Mapperator.ConsoleApp.Verbs;

public static class Extract2 {
    [Verb("extract2", HelpText = "Extract beatmap data from an osu! collection to ML data.")]
    public class Extract2Options : FilterBase {
        [Option('o', "output", Required = true, HelpText = "Filename of the output.")]
        public string? OutputName { get; [UsedImplicitly] set; }
    }

    public static int DoDataExtraction(Extract2Options opts) {
        if (opts.OutputName is null) throw new ArgumentNullException(nameof(opts));

        bool[] mirrors = { false, true };
        var extractor = new DataExtractor2();
        File.WriteAllLines(Path.ChangeExtension(opts.OutputName, ".txt"),
            DataSerializer2.SerializeBeatmapData(DbManager.GetFilteredAndRead2(opts)
                .Select(b => (extractor.ExtractBeatmapData(b.Item1), b.Item1.Difficulty, b.Item2.BeatmapId))
            ));

        return 0;
    }
}