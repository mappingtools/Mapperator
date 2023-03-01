using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;
using Mapperator.ConsoleApp.Resources;
using Mapping_Tools_Core.Audio;

namespace Mapperator.ConsoleApp.Verbs;

public static class Count {
    [Verb("count", HelpText = "Count the amount of beatmaps available matching the specified filter.")]
    public class CountOptions : FilterBase {
        [Option('u', "uniqueSong", HelpText = "Count each unique song file", Default = false)]
        public bool UniqueSong { get; [UsedImplicitly] set; }

        [Option('f', "fileSize", HelpText = "Aggregate the filesize of the songs", Default = false)]
        public bool FileSize { get; [UsedImplicitly] set; }

        [Option('v', "verbose", HelpText = "Print the name of each counted beatmap", Default = false)]
        public bool Verbose { get; [UsedImplicitly] set; }
    }

    public static int DoDataCount(CountOptions opts) {
        var songNames = new HashSet<string>();
        long totalSize = 0;
        var totalTime = TimeSpan.Zero;

        Console.WriteLine(DbManager.GetFiltered(opts)
            .Count(o => {
                if (opts.Verbose) Console.WriteLine(Strings.FullBeatmapName, o.Artist, o.Title, o.Creator, o.Difficulty);
                if (!opts.UniqueSong) return true;
                string songFile = Path.Combine(ConfigManager.Config.SongsPath, o.FolderName.Trim(), o.AudioFileName.Trim());
                string songName = $"{o.Artist} - {RemovePartsBetweenParentheses(o.Title)}";
                if (!string.Equals(Path.GetExtension(songFile), ".mp3", StringComparison.OrdinalIgnoreCase)) return false;
                if (songNames.Contains(songName)) return false;
                songNames.Add(songName);
                var info = new FileInfo(songFile);
                if (!info.Exists) return false;
                if (opts.FileSize) {
                    totalSize += info.Length;
                    try {
                        totalTime += new Mp3FileReader(songFile).TotalTime;
                    } catch (InvalidOperationException e) {
                        Console.WriteLine(e);
                        return false;
                    }
                }
                if (opts.Verbose) Console.WriteLine(songName);
                return true;
            }));

        if (opts.FileSize) {
            Console.WriteLine(Strings.Count_DoDataCount_Total_file_size___0__MB, totalSize / 1024 / 1024);
            Console.WriteLine(Strings.Count_DoDataCount_Total_duration___0_, totalTime);
        }

        return 0;
    }

    private static string RemovePartsBetweenParentheses(string str) {
        Span<char> span = stackalloc char[str.Length];
        int j = 0;
        for (int i = 0; i < str.Length; i++) {
            if (str[i] == '(') {
                if (i > 0 && str[i - 1] == ' ') j--;
                while (str[i] != ')') i++;
                continue;
            }
            span[j++] = str[i];
        }
        return span[..j].ToString();
    }
}