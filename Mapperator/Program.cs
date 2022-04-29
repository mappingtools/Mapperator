using CommandLine;
using HNSW.Net;
using Mapperator.Matching;
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
using Mapperator.Construction;

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

        [Verb("build", HelpText = "Build a data structure using extracted beatmap data.")]
        class BuildOptions {
            [Option('d', "data", Required = true, HelpText = "Input extracted beatmap data for the graph.")]
            public string DataPath { get; set; }

            [Option('h', "structOutput", Required = true, HelpText = "Filename for the generated data structure.")]
            public string OutputStructName { get; set; }

            [Option('m', "matcher", Default = MatcherType.HNSW, HelpText = "The type of data matcher to use.")]
            public MatcherType MatcherType { get; set; }
        }

        [Verb("convert", HelpText = "Reconstruct a beatmap using extracted beatmap data.")]
        class ConvertOptions {
            [Option('d', "data", Required = true, HelpText = "Input extracted beatmap data for the conversion.")]
            public string DataPath { get; set; }

            [Option('i', "input", Required = true, HelpText = "Input beatmap to be converted.")]
            public string InputBeatmapPath { get; set; }

            [Option('o', "output", Required = true, HelpText = "Filename of the output.")]
            public string OutputName { get; set; }

            [Option('g', "structInput", HelpText = "Serialized data structure file to speed up matching.")]
            public string InputStructName { get; set; }

            [Option('h', "structOutput", HelpText = "Filename for the generated data structure.")]
            public string OutputStructName { get; set; }

            [Option('m', "matcher", Default = MatcherType.HNSW, HelpText = "The type of data matcher to use.")]
            public MatcherType MatcherType { get; set; }
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

        private static IDataMatcher GetDataMatcher(MatcherType matcherType) {
            return matcherType switch {
                MatcherType.Simple => new SimpleDataMatcher(),
                MatcherType.HNSW => new HnswDataMatcher(),
                _ => throw new NotImplementedException()
            };
        }

        private static int DoBuildGraph(BuildOptions opts) {
            var trainData = DataSerializer.DeserializeBeatmapData(File.ReadLines(Path.ChangeExtension(opts.DataPath, ".txt"))).ToList();
            IDataMatcher matcher = GetDataMatcher(opts.MatcherType);

            if (matcher is not ISerializable sMatcher) {
                Console.WriteLine($"The {opts.MatcherType} matcher is not compatible with building.");
                return 0;
            }

            matcher.AddData(trainData);
            using Stream file = File.Create(Path.ChangeExtension(opts.OutputStructName, sMatcher.DefaultExtension));
            sMatcher.Save(file);
            return 0;
        }

        private static int DoMapConvert(ConvertOptions opts) {
            Console.WriteLine("Extracting data...");
            var trainData = DataSerializer.DeserializeBeatmapData(File.ReadLines(Path.ChangeExtension(opts.DataPath, ".txt")));
            var map = new BeatmapEditor(Path.ChangeExtension(opts.InputBeatmapPath, ".osu")).ReadFile();
            var input = new DataExtractor().ExtractBeatmapData(map).ToList();

            IDataMatcher matcher = GetDataMatcher(opts.MatcherType);

            // Add the data to the matcher or load the data
            if (matcher is ISerializable sMatcher && 
                !string.IsNullOrEmpty(opts.InputStructName) && 
                File.Exists(Path.ChangeExtension(opts.InputStructName, sMatcher.DefaultExtension))) {
                using Stream file = File.OpenRead(Path.ChangeExtension(opts.InputStructName, sMatcher.DefaultExtension));
                sMatcher.Load(trainData, file);
            } else {
                matcher.AddData(trainData);
                if (matcher is ISerializable sMatcher2 && !string.IsNullOrEmpty(opts.OutputStructName)) {
                    using Stream file = File.Create(Path.ChangeExtension(opts.OutputStructName, sMatcher2.DefaultExtension));
                    sMatcher2.Save(file);
                }
            }

            // Construct new beatmap
            Console.WriteLine("Constructing beatmap...");
            map.Metadata.Version = "Converted";
            map.HitObjects.Clear();
            map.Editor.Bookmarks.Clear();
            var constructor = new BeatmapConstructor();
            constructor.PopulateBeatmap(map, input, matcher);

            new BeatmapEditor(Path.ChangeExtension(opts.OutputName, ".osu")).WriteFile(map);
            return 0;
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
    }
}
