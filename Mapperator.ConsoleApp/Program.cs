using CommandLine;
using Mapping_Tools_Core;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using Mapping_Tools_Core.MathUtil;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mapperator.ConsoleApp.Resources;
using Mapperator.Construction;
using Mapperator.Matching;
using Mapperator.Matching.DataStructures;
using Mapperator.Matching.Matchers;
using Mapperator.Model;
using OsuParsers.Database.Objects;
using OsuParsers.Enums;
using OsuParsers.Enums.Database;

namespace Mapperator.ConsoleApp {
    public static class Program {
        [Verb("count", HelpText = "Count the amount of beatmaps available matching the specified filter.")]
        private class CountOptions : IHasFilter {
            [Option('c', "collection", HelpText = "Name of osu! collection to be extracted.")]
            public string? CollectionName { get; set; }

            [Option('i', "minId", HelpText = "Filter the minimum beatmap set ID.")]
            public int? MinId { get; set; }

            [Option('s', "status", HelpText = "Filter the ranked status.")]
            public RankedStatus? RankedStatus { get; set; }

            [Option('m', "mode", HelpText = "Filter the game mode.")]
            public Ruleset? Ruleset { get; set; }

            [Option('r', "starRating", HelpText = "Filter the star rating.")]
            public double? MinStarRating { get; set; }

            [Option('a', "mapper", HelpText = "Filter on mapper name.")]
            public string? Mapper { get; set; }

            [Option('v', "verbose", HelpText = "Print the name of each counted beatmap", Default = false)]
            public bool Verbose { get; set; }
        }

        [Verb("extract", HelpText = "Extract beatmap data from an osu! collection.")]
        private class ExtractOptions : IHasFilter {
            [Option('c', "collection", HelpText = "Name of osu! collection to be extracted.")]
            public string? CollectionName { get; set; }

            [Option('i', "minId", HelpText = "Filter the minimum beatmap set ID.")]
            public int? MinId { get; set; }

            [Option('s', "status", HelpText = "Filter the ranked status.")]
            public RankedStatus? RankedStatus { get; set; }

            [Option('m', "mode", HelpText = "Filter the game mode.")]
            public Ruleset? Ruleset { get; set; }

            [Option('r', "starRating", HelpText = "Filter the star rating.")]
            public double? MinStarRating { get; set; }

            [Option('a', "mapper", HelpText = "Filter on mapper name.")]
            public string? Mapper { get; set; }

            [Option('o', "output", Required = true, HelpText = "Filename of the output.")]
            public string? OutputName { get; set; }
        }

        [Verb("build", HelpText = "Build a data structure using extracted beatmap data.")]
        private class BuildOptions {
            [Option('d', "data", Required = true, HelpText = "Input extracted beatmap data for the graph.")]
            public string? DataPath { get; set; }

            [Option('h', "structOutput", Required = true, HelpText = "Filename for the generated data structure.")]
            public string? OutputStructName { get; set; }
        }

        [Verb("convert", HelpText = "Reconstruct a beatmap using extracted beatmap data.")]
        private class ConvertOptions {
            [Option('d', "data", Required = true, HelpText = "Input extracted beatmap data for the conversion.")]
            public string? DataPath { get; set; }

            [Option('i', "input", Required = true, HelpText = "Input beatmap to be converted.")]
            public string? InputBeatmapPath { get; set; }

            [Option('o', "output", Required = true, HelpText = "Filename of the output.")]
            public string? OutputName { get; set; }

            [Option('g', "structInput", HelpText = "Serialized data structure file to speed up matching.")]
            public string? InputStructName { get; set; }

            [Option('h', "structOutput", HelpText = "Filename for the generated data structure.")]
            public string? OutputStructName { get; set; }

            [Option('s', "spacingMap", HelpText = "Filename a beatmap with the desired spacing distribution.")]
            public string? SpacingBeatmapPath { get; set; }
        }

        [Verb("search", HelpText = "Search your entire Songs folder for a specific pattern.")]
        class SearchOptions {
            [Option('p', "pattern", Required = true, HelpText = "Prints all messages to standard output.")]
            public string? Pattern { get; set; }

            [Option('c', "collection", HelpText = "Name of osu! collection to be searched.")]
            public string? CollectionName { get; set; }
        }

        private static int Main(string[] args) {
            ConfigManager.LoadConfig();

            return Parser.Default.ParseArguments<CountOptions, ExtractOptions, BuildOptions, ConvertOptions, SearchOptions>(args)
              .MapResult(
                  (CountOptions opts) => DoDataCount(opts),
                (ExtractOptions opts) => DoDataExtraction(opts),
                (BuildOptions opts) => DoBuildGraph(opts),
                (ConvertOptions opts) => DoMapConvert(opts),
                (SearchOptions opts) => DoPatternSearch(opts),
                _ => 1);
        }

        private static int DoPatternSearch(SearchOptions opts) {
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

        private static int DoBuildGraph(BuildOptions opts) {
            if (opts.OutputStructName is null) throw new ArgumentNullException(nameof(opts));
            if (opts.DataPath is null) throw new ArgumentNullException(nameof(opts));

            var trainData = DataSerializer.DeserializeBeatmapData(File.ReadLines(Path.ChangeExtension(opts.DataPath, ".txt")));
            var data = new RhythmDistanceTrieStructure();

            if (data is not ISerializable sMatcher) {
                Console.WriteLine(Strings.Program_DoBuildGraph_The__0__matcher_is_not_compatible_with_building_);
                return 0;
            }

            foreach (var str in trainData) {
                data.Add(str.ToArray());
            }

            using Stream file = File.Create(Path.ChangeExtension(opts.OutputStructName, sMatcher.DefaultExtension));
            sMatcher.Save(file);
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

        private static int DoMapConvert(ConvertOptions opts) {
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
            // TODO: create some kind of procedural slider genration by splitting each slider segment into a separate data point
            // TODO: add post-processing filters for fixing overlaps, stacks, and slider angles
            // Change spacing distribution
            if (opts.SpacingBeatmapPath is not null) {
                Console.WriteLine(Strings.Program_DoMapConvert_Converting_spacing_to_reference_beatmap___);
                var spacingMap = new BeatmapEditor(Path.ChangeExtension(opts.SpacingBeatmapPath, ".osu")).ReadFile();
                var spacingMapData = new DataExtractor().ExtractBeatmapData(spacingMap).ToArray();
                input = TransferSpacing(spacingMapData, input);
            }

            var data = new RhythmDistanceTrieStructure();
            var matcher = new TrieDataMatcher(data, input);

            // Add the data to the matcher or load the data
            Console.WriteLine(Strings.Program_DoMapConvert_Adding_data___);
            if (data is ISerializable sMatcher &&
                !string.IsNullOrEmpty(opts.InputStructName) && 
                File.Exists(Path.ChangeExtension(opts.InputStructName, sMatcher.DefaultExtension))) {
                using Stream file = File.OpenRead(Path.ChangeExtension(opts.InputStructName, sMatcher.DefaultExtension));
                sMatcher.Load(trainData, file);
            } else {
                Stopwatch buildStopwatch = new Stopwatch();
                buildStopwatch.Start();

                foreach (var str in trainData) {
                    data.Add(str.ToArray());
                    Console.Write('.');
                }

                buildStopwatch.Stop();
                Console.WriteLine(Strings.Program_DoMapConvert_Elapsed_Time_is, buildStopwatch.ElapsedMilliseconds.ToString());

                if (matcher is ISerializable sMatcher2 && !string.IsNullOrEmpty(opts.OutputStructName)) {
                    using Stream file = File.Create(Path.ChangeExtension(opts.OutputStructName, sMatcher2.DefaultExtension));
                    sMatcher2.Save(file);
                }
            }

            // Construct new beatmap
            Console.WriteLine(Strings.Program_DoMapConvert_Constructing_beatmap___);
            map.Metadata.Version = "Converted";
            map.HitObjects.Clear();
            map.Editor.Bookmarks.Clear();
            var constructor = new BeatmapConstructor();
            constructor.PopulateBeatmap(map, input, matcher);

            new BeatmapEditor(Path.ChangeExtension(opts.OutputName, ".osu")).WriteFile(map);

            // Print elapsed time
            stopwatch.Stop();
            Console.WriteLine(Strings.Program_DoMapConvert_Elapsed_Time_is, stopwatch.ElapsedMilliseconds.ToString());

            return 0;
        }

        private static int DoDataCount(CountOptions opts) {
            Console.WriteLine((opts.CollectionName is null ? DbManager.GetAll() : DbManager.GetCollection(opts.CollectionName))
                .Where(o => DbBeatmapFilter(o, opts))
                .Count(o => { if (opts.Verbose) Console.WriteLine(Strings.FullBeatmapName, o.Artist, o.Title, o.Creator, o.Difficulty);
                    return true;
                }));
            return 0;
        }

        private static int DoDataExtraction(ExtractOptions opts) {
            if (opts.OutputName is null) throw new ArgumentNullException(nameof(opts));

            bool[] mirrors = { false, true };
            var extractor = new DataExtractor();
            File.WriteAllLines(Path.ChangeExtension(opts.OutputName, ".txt"),
                DataSerializer.SerializeBeatmapData((opts.CollectionName is null ? DbManager.GetAll() : DbManager.GetCollection(opts.CollectionName))
                .Where(o => DbBeatmapFilter(o, opts))
                .Select(o => Path.Combine(ConfigManager.Config.SongsPath, o.FolderName.Trim(), o.FileName.Trim()))
                .Where(o => {
                    if (File.Exists(o)) {
                        Console.Write('.');
                        return true;
                    }
                    Console.WriteLine(Strings.CouldNotFindFile, o);
                    return false;
                })
                .Select(o => {
                    try {
                        return new BeatmapEditor(o).ReadFile();
                    }
                    catch (Exception e) {
                        Console.WriteLine(Strings.ErrorReadingFile, o, e);
                        return null;
                    }
                }).Where(o => o is not null)
                .SelectMany(b => mirrors.Select(m => extractor.ExtractBeatmapData(b!, m)))
                ));
            return 0;
        }

        private static bool DbBeatmapFilter(DbBeatmap o, IHasFilter opts) {
            // Regex which matches any diffname with a possessive indicator to anyone other than the mapper
            var regex = opts.Mapper is not null ? new Regex(@$"(?!\s?(de\s)?(it|that|{Regex.Escape(opts.Mapper)}))(((^|[^\S\r\n])(\S)*([sz]'|'s))|((^|[^\S\r\n])de\s(\S)*))", RegexOptions.IgnoreCase) : null;

            return (!opts.MinId.HasValue || o.BeatmapSetId >= opts.MinId)
                   && (!opts.RankedStatus.HasValue || o.RankedStatus == opts.RankedStatus)
                   && (!opts.Ruleset.HasValue || o.Ruleset == opts.Ruleset)
                   && (!opts.MinStarRating.HasValue || GetDefaultStarRating(o) >= opts.MinStarRating)
                   && (opts.Mapper is null || ((o.Creator == opts.Mapper || o.Difficulty.Contains(opts.Mapper))
                                               && !o.Difficulty.Contains("Hitsounds", StringComparison.OrdinalIgnoreCase)
                                               && !o.Difficulty.Contains("Collab", StringComparison.OrdinalIgnoreCase)
                                               && !regex!.IsMatch(o.Difficulty)));
        }

        private static double GetDefaultStarRating(DbBeatmap beatmap) {
            return beatmap.Ruleset switch {
                Ruleset.Taiko => beatmap.TaikoStarRating[Mods.None],
                Ruleset.Mania => beatmap.ManiaStarRating[Mods.None],
                Ruleset.Fruits => beatmap.CatchStarRating[Mods.None],
                _ => beatmap.StandardStarRating[Mods.None]
            };
        }
    }

    internal interface IHasFilter {
        int? MinId { get; }
        RankedStatus? RankedStatus { get; }
        Ruleset? Ruleset { get; }
        double? MinStarRating { get; }
        string? Mapper { get; }
    }
}
