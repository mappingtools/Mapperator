﻿using Mapperator.Resources;
using Mapping_Tools_Core;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using Mapping_Tools_Core.MathUtil;
using System;
using System.IO;
using System.Linq;

namespace Mapperator {
    public class Program {
        static void Main(string[] args) {
            ConfigManager.LoadConfig();

            if (args.Length == 0) {
                // Show info about the program and basic usage
                Console.WriteLine(Strings.About);
                return;
            }

            string collectionName;
            string inputName;
            string outputName;
            string dataName;
            switch(args[0]) {
                case "-e":
                    if (!CheckLength(args, 3)) return;                   ;
                    collectionName = args[1];
                    outputName = args[2];
                    DoDataExtraction(collectionName, outputName);
                    break;
                case "-c":
                    if (!CheckLength(args, 4)) return;
                    dataName = args[1];
                    inputName = args[2];
                    outputName = args[3];
                    DoMapConvert(dataName, inputName, outputName);
                    break;
                case "-s":
                    if (!CheckLength(args, 2)) return;
                    string pattern = args[1].Replace("\"", ""); ;
                    DoPatternSearch(pattern);
                    break;
                default:
                    Console.WriteLine(string.Format(Strings.UnknownCommand, args[0]));
                    break;
            }
        }

        private static void DoPatternSearch(string pattern) {
            int matches = 0;
            int i = 0;
            foreach (var path in Directory.EnumerateFiles(ConfigManager.Config.SongsPath, "*.osu",
                new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true, ReturnSpecialDirectories = false })) {
                PatternSearchMap(path, pattern, i++, ref matches);
            }
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

        private static void DoMapConvert(string dataName, string inputName, string outputName) {
            var data = DataSerializer.DeserializeBeatmapData(File.ReadLines(Path.ChangeExtension(dataName, ".txt")));
        }

        static void DoDataExtraction(string collectionName, string outputName) {
            File.WriteAllLines(Path.ChangeExtension(outputName, ".txt"),
                DbManager.GetCollection(collectionName)
                .Select(o => new BeatmapEditor(Path.Combine(ConfigManager.Config.SongsPath, o.FolderName, o.FileName)).ReadFile())
                .SelectMany(DataExtractor.ExtractBeatmapData)
                .Select(DataSerializer.SerializeBeatmapDataSample));
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