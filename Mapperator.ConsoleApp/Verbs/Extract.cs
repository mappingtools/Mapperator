using System;
using System.IO;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;

namespace Mapperator.ConsoleApp.Verbs;

public static class Extract {
    [Verb("extract", HelpText = "Extract beatmap data from an osu! collection.")]
    public class ExtractOptions : FilterBase {
        [Option('o', "output", Required = true, HelpText = "Filename of the output.")]
        public string? OutputName { get; [UsedImplicitly] set; }
    }

    public static int DoDataExtraction(ExtractOptions opts) {
        if (opts.OutputName is null) throw new ArgumentNullException(nameof(opts));

        bool[] mirrors = { false, true };
        var extractor = new DataExtractor();
        File.WriteAllLines(Path.ChangeExtension(opts.OutputName, ".txt"),
            DataSerializer.SerializeBeatmapData(DbManager.GetFilteredAndRead(opts)
                .SelectMany(b => mirrors.Select(m => extractor.ExtractBeatmapData(b, m)))
            ).Prepend(DataSerializer.CurrentHeader));

        return 0;
    }
}