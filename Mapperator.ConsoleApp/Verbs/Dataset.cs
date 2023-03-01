using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;
using JetBrains.Annotations;
using Mapperator.ConsoleApp.Resources;
using Mapping_Tools_Core.Audio;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using OsuParsers.Database.Objects;
using OsuParsers.Enums;
using OsuParsers.Enums.Database;

namespace Mapperator.ConsoleApp.Verbs;

public static class Dataset {
    [Verb("dataset", HelpText = "Extract a ML dataset from your osu! database.")]
    public class DatasetOptions : FilterBase {
        [Option('o', "output", Required = true, HelpText = "Folder to output the dataset to.")]
        public string? OutputFolder { get; [UsedImplicitly] set; }
    }

    public static int DoDataExtraction(DatasetOptions opts) {
        if (opts.OutputFolder is null) throw new ArgumentNullException(nameof(opts));

        if (!Directory.Exists(opts.OutputFolder)) {
            Directory.CreateDirectory(opts.OutputFolder);
        }

        Console.WriteLine(Strings.Dataset_DoDataExtraction_Finding_beatmap_sets___);

        var mapSets = new Dictionary<(string, string), (int, string)>();
        long totalSize = 0;
        var totalTime = TimeSpan.Zero;

        foreach (var o in DbManager.GetFiltered(opts)) {
            string songFile = Path.Combine(ConfigManager.Config.SongsPath, o.FolderName.Trim(), o.AudioFileName.Trim());
            string mapFile = Path.Combine(ConfigManager.Config.SongsPath, o.FolderName.Trim(), o.FileName.Trim());
            var songName = (o.Artist, RemovePartsBetweenParentheses(o.Title));

            if (!string.Equals(Path.GetExtension(songFile), ".mp3", StringComparison.OrdinalIgnoreCase)) continue;
            if (mapSets.ContainsKey(songName)) continue;

            var info = new FileInfo(songFile);
            if (!info.Exists) continue;

            totalSize += info.Length;
            try {
                totalTime += new Mp3FileReader(songFile).TotalTime;
            } catch (InvalidOperationException e) {
                Console.WriteLine(e);
                continue;
            }

            if (!File.Exists(mapFile)) continue;
            try {
                new BeatmapEditor(mapFile).ReadFile();
            } catch (Exception e) {
                Console.WriteLine(Strings.ErrorReadingFile, mapFile, e);
                continue;
            }

            mapSets.Add(songName, (o.BeatmapSetId, songFile));
            Console.Write('\r');
            Console.Write(Strings.Dataset_DoDataExtraction_Count_Update, mapSets.Count);
        }

        Console.WriteLine();
        Console.WriteLine(Strings.Count_DoDataCount_Total_file_size___0__MB, totalSize / 1024 / 1024);
        Console.WriteLine(Strings.Count_DoDataCount_Total_duration___0_, totalTime);
        Console.WriteLine(Strings.Dataset_DoDataExtraction_Writing_dataset___);

        const string mapSubFolder = "beatmaps";
        const string audioName = "audio.mp3";
        const string metadataName = "metadata.json";
        var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new DictionaryConverter() }};

        var sortedMapSets = mapSets.Values.OrderBy(o => o.Item1).ToArray();
        var db = DbManager.GetOsuDatabase();
        int mapSetCount = 0;
        for (var i = 0; i < sortedMapSets.Length; i++) {
            var (mapSetId, songFile) = sortedMapSets[i];
            var maps = DbManager.GetMapSet(db, mapSetId).OrderBy(DbManager.GetDefaultStarRating).ToArray();
            if (maps.Length == 0) continue;

            string mapSetFolderName = $"Track{mapSetCount:D5}";
            string mapSetFolderPath = Path.Combine(opts.OutputFolder, mapSetFolderName);
            Directory.CreateDirectory(mapSetFolderPath);
            Directory.CreateDirectory(Path.Combine(mapSetFolderPath, mapSubFolder));

            var mapCount = 0;
            DbBeatmap? lastMap = null;
            var beatmapMetadatas = new Dictionary<string, BeatmapMetadata>();
            foreach (var dbBeatmap in maps) {
                if (!DbManager.DbBeatmapFilter(dbBeatmap, opts)) continue;

                string mapFile = Path.Combine(ConfigManager.Config.SongsPath, dbBeatmap.FolderName.Trim(), dbBeatmap.FileName.Trim());
                if (!File.Exists(mapFile)) continue;
                try {
                    var editor = new BeatmapEditor(mapFile);
                    var beatmap = editor.ReadFile();
                    ClearStoryboard(beatmap.Storyboard);

                    string mapName = $"M{mapCount:D3}";
                    string mapOutputName = Path.Combine(opts.OutputFolder, mapSetFolderName, mapSubFolder, mapName + ".osu");
                    editor.Path = mapOutputName;
                    editor.WriteFile(beatmap);

                    beatmapMetadatas.Add(mapName, new BeatmapMetadata(dbBeatmap.BeatmapId, dbBeatmap.OnlineOffset, dbBeatmap.DrainTime, dbBeatmap.TotalTime, dbBeatmap.RankedStatus, dbBeatmap.StandardStarRating, dbBeatmap.TaikoStarRating, dbBeatmap.CatchStarRating, dbBeatmap.ManiaStarRating));

                    mapCount++;
                    lastMap = dbBeatmap;
                } catch (Exception e) {
                    Console.WriteLine(Strings.ErrorReadingFile, mapFile, e);
                }
            }

            if (lastMap is null) {
                // There are no valid maps for this song so delete the mapset folder
                Directory.Delete(mapSetFolderPath, true);
                continue;
            }

            // Write metadata
            var metadata = new Metadata(mapSetId, lastMap.Artist, lastMap.Title, audioName, mapSubFolder, beatmapMetadatas);
            string json = JsonSerializer.Serialize(metadata, options);
            File.WriteAllText(Path.Combine(mapSetFolderPath, metadataName), json);

            // Copy audio file
            File.Copy(songFile, Path.Combine(opts.OutputFolder, mapSetFolderName, audioName), true);

            Console.Write('\r');
            Console.Write(Strings.Dataset_DoDataExtraction_Copy_Update, i + 1, sortedMapSets.Length);
            mapSetCount++;
        }

        return 0;
    }

    private static void ClearStoryboard(IStoryboard sb) {
        sb.BackgroundColourTransformations.Clear();
        sb.StoryboardLayerBackground.Clear();
        sb.StoryboardLayerFail.Clear();
        sb.StoryboardLayerForeground.Clear();
        sb.StoryboardLayerOverlay.Clear();
        sb.StoryboardLayerPass.Clear();
        sb.StoryboardSoundSamples.Clear();
        sb.BackgroundAndVideoEvents.Clear();
    }

    public static string RemovePartsBetweenParentheses(string str) {
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

    private class DictionaryConverter : JsonConverter<Dictionary<Mods, double>> {
        public override Dictionary<Mods, double> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<Mods, double> dictionary, JsonSerializerOptions options) {
            writer.WriteStartObject();
            foreach ((var key, double value) in dictionary) {
                writer.WritePropertyName(((int)key).ToString(CultureInfo.InvariantCulture));
                writer.WriteNumberValue(value);
            }
            writer.WriteEndObject();
        }
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private record Metadata(int BeatmapSetId, string Artist, string Title, string AudioFile, string MapDir, Dictionary<string, BeatmapMetadata> Beatmaps);

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private record BeatmapMetadata(int BeatmapId, short OnlineOffset, int DrainTime, int TotalTime, RankedStatus RankedStatus, Dictionary<Mods, double> StandardStarRating, Dictionary<Mods, double> TaikoStarRating, Dictionary<Mods, double> CatchStarRating, Dictionary<Mods, double> ManiaStarRating);
}