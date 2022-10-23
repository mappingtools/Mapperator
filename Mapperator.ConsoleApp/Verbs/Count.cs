using System;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;
using Mapperator.ConsoleApp.Resources;

namespace Mapperator.ConsoleApp.Verbs;

public static class Count {
    [Verb("count", HelpText = "Count the amount of beatmaps available matching the specified filter.")]
    public class CountOptions : FilterBase {
        [Option('v', "verbose", HelpText = "Print the name of each counted beatmap", Default = false)]
        public bool Verbose { get; [UsedImplicitly] set; }
    }

    public static int DoDataCount(CountOptions opts) {
        Console.WriteLine(DbManager.GetFiltered(opts)
            .Count(o => { if (opts.Verbose) Console.WriteLine(Strings.FullBeatmapName, o.Artist, o.Title, o.Creator, o.Difficulty);
                return true;
            }));

        return 0;
    }
}