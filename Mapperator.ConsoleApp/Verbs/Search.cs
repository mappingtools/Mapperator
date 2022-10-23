using System;
using System.IO;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;
using Mapperator.ConsoleApp.Resources;
using Mapping_Tools_Core;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.ConsoleApp.Verbs;

public static class Search {
    [Verb("search", HelpText = "Search your entire Songs folder for a specific pattern.")]
    public class SearchOptions {
        [Option('p', "pattern", Required = true, HelpText = "Prints all messages to standard output.")]
        public string? Pattern { get; [UsedImplicitly] set; }

        [Option('c', "collection", HelpText = "Name of osu! collection to be searched.")]
        public string? CollectionName { get; [UsedImplicitly] set; }
    }

    public static int DoPatternSearch(SearchOptions opts) {
        var matches = 0;
        var i = 0;
        foreach (var path in string.IsNullOrEmpty(opts.CollectionName) ? Directory.EnumerateFiles(ConfigManager.Config.SongsPath, "*.osu",
                         new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true, ReturnSpecialDirectories = false }) :
                     DbManager.GetCollection(opts.CollectionName).Select(o => Path.Combine(ConfigManager.Config.SongsPath, o.FolderName, o.FileName))) {
            PatternSearchMap(path, opts.Pattern, i++, ref matches);
        }

        return 0;
    }

    private static void PatternSearchMap(string path, string? pattern, int i, ref int matches) {
        if (pattern is null) throw new ArgumentNullException(nameof(pattern));

        if (i % 1000 == 0) {
            Console.Write('.');
        }
        //Console.WriteLine(path);

        var startBracketIndex = pattern.IndexOf("(", StringComparison.Ordinal);
        var endBracketIndex = pattern.IndexOf(")", StringComparison.Ordinal);
        var t = InputParsers.ParseOsuTimestamp(pattern).TotalMilliseconds;
        var l = 0;
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
            if (hos.Length != l || !Precision.AlmostEquals(hos[0].StartTime, t)) return;
            matches++;
            Console.WriteLine(Strings.Program_PatternSearchMap_Found_match__0__in_beatmap___1_, matches, path);
        } catch (Exception e) {
            Console.WriteLine(Strings.Program_PatternSearchMap_Can_t_parse_this_map__ + path);
            Console.WriteLine(e);
        }
    }
}