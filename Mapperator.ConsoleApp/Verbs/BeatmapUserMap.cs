using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommandLine;
using JetBrains.Annotations;
using Mapperator.ConsoleApp.Resources;
using Mapperator.ConsoleApp.Utils;
using Microsoft.VisualBasic.FileIO;

namespace Mapperator.ConsoleApp.Verbs;

public static class BeatmapUserMap {
    [Verb("beatmap-user-map", HelpText = "Read metadata.csv, fetch beatmapsets from the osu! v2 API, and write a JSON mapping of beatmap_id to user_id.")]
    public class BeatmapUserMapOptions {
        [Option('i', "input", Required = true, HelpText = "Path to metadata.csv.")]
        public string? MetadataCsvPath { get; [UsedImplicitly] set; }

        [Option('o', "output", Required = true, HelpText = "Path to the output JSON file.")]
        public string? OutputJsonPath { get; [UsedImplicitly] set; }

        [Option('c', "cache", Required = false, HelpText = "Path to the osu! beatmapset cache file. Defaults to online_cache.jsonl in the working directory.")]
        public string? OnlineCachePath { get; [UsedImplicitly] set; }
    }

    public static int DoBeatmapUserMap(BeatmapUserMapOptions args) {
        if (string.IsNullOrWhiteSpace(args.MetadataCsvPath)) {
            throw new ArgumentNullException(nameof(args.MetadataCsvPath));
        }

        if (string.IsNullOrWhiteSpace(args.OutputJsonPath)) {
            throw new ArgumentNullException(nameof(args.OutputJsonPath));
        }

        var beatmapIdsByBeatmapSetId = ReadBeatmapIdsByBeatmapSetId(args.MetadataCsvPath);
        int beatmapCount = beatmapIdsByBeatmapSetId.Values.Sum(beatmapIds => beatmapIds.Count);

        Console.WriteLine(Strings.BeatmapUserMap_DoBeatmapUserMap_Loaded__0__beatmap_IDs_across__1__beatmapsets_from___2___, beatmapCount, beatmapIdsByBeatmapSetId.Count,
            args.MetadataCsvPath);

        Dictionary<int, int> beatmapUserIds;
        using (var osuApiClient = new OsuApiV2Client(
                   ConfigManager.Config.ClientId,
                   ConfigManager.Config.ClientSecret,
                   args.OnlineCachePath)) {
            beatmapUserIds = BuildBeatmapUserIds(beatmapIdsByBeatmapSetId, osuApiClient);
        }

        string? outputDirectory = Path.GetDirectoryName(args.OutputJsonPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory)) {
            Directory.CreateDirectory(outputDirectory);
        }

        var jsonOptions = new JsonSerializerOptions {
            WriteIndented = true
        };
        File.WriteAllText(args.OutputJsonPath, JsonSerializer.Serialize(beatmapUserIds, jsonOptions));

        Console.WriteLine(Strings.BeatmapUserMap_DoBeatmapUserMap_Wrote__0__beatmap_user_mappings_to___1___, beatmapUserIds.Count, args.OutputJsonPath);
        return 0;
    }

    public static Dictionary<int, List<int>> ReadBeatmapIdsByBeatmapSetId(string metadataCsvPath) {
        if (!File.Exists(metadataCsvPath)) {
            throw new FileNotFoundException("metadata.csv file not found.", metadataCsvPath);
        }

        using var parser = new TextFieldParser(metadataCsvPath);
        parser.TextFieldType = FieldType.Delimited;
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = true;
        parser.SetDelimiters(",");

        try {
            if (parser.EndOfData) {
                throw new InvalidDataException("metadata.csv is empty.");
            }

            string[] headers = parser.ReadFields() ?? throw new InvalidDataException("metadata.csv is empty.");
            int beatmapIdIndex = FindRequiredHeaderIndex(headers, "beatmap_id");
            int beatmapsetIdIndex = FindRequiredHeaderIndex(headers, "beatmapset_id");

            var beatmapIdsByBeatmapSetId = new Dictionary<int, List<int>>();
            var beatmapSetIdsByBeatmapId = new Dictionary<int, int>();

            while (!parser.EndOfData) {
                long lineNumber = parser.LineNumber;
                string[]? values = parser.ReadFields();
                if (values is null || values.All(string.IsNullOrWhiteSpace)) {
                    continue;
                }

                if (values.Length <= Math.Max(beatmapIdIndex, beatmapsetIdIndex)) {
                    throw new InvalidDataException($"Line {lineNumber} does not contain both beatmap_id and beatmapset_id.");
                }

                if (!int.TryParse(values[beatmapIdIndex], out int beatmapId)) {
                    throw new InvalidDataException($"Line {lineNumber} has an invalid beatmap_id value: '{values[beatmapIdIndex]}'.");
                }

                if (!int.TryParse(values[beatmapsetIdIndex], out int beatmapsetId)) {
                    throw new InvalidDataException($"Line {lineNumber} has an invalid beatmapset_id value: '{values[beatmapsetIdIndex]}'.");
                }

                if (beatmapSetIdsByBeatmapId.TryGetValue(beatmapId, out int existingBeatmapsetId)) {
                    if (existingBeatmapsetId != beatmapsetId) {
                        throw new InvalidDataException($"Beatmap {beatmapId} is associated with multiple beatmapsets: {existingBeatmapsetId} and {beatmapsetId}.");
                    }

                    continue;
                }

                beatmapSetIdsByBeatmapId[beatmapId] = beatmapsetId;
                if (!beatmapIdsByBeatmapSetId.TryGetValue(beatmapsetId, out var beatmapIds)) {
                    beatmapIds = [];
                    beatmapIdsByBeatmapSetId[beatmapsetId] = beatmapIds;
                }

                beatmapIds.Add(beatmapId);
            }

            return beatmapIdsByBeatmapSetId;
        }
        catch (MalformedLineException ex) {
            long lineNumber = parser.ErrorLineNumber > 0 ? parser.ErrorLineNumber : parser.LineNumber;
            throw new InvalidDataException($"Line {lineNumber} is not valid CSV: {ex.Message}", ex);
        }
    }

    public static Dictionary<int, int> BuildBeatmapUserIds(IReadOnlyDictionary<int, List<int>> beatmapIdsByBeatmapSetId, OsuApiV2Client client) {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(beatmapIdsByBeatmapSetId);

        int totalBeatmaps = beatmapIdsByBeatmapSetId.Values.Sum(beatmapIds => beatmapIds.Count);
        var result = new Dictionary<int, int>(totalBeatmaps);
        var i = 0;

        foreach ((int beatmapsetId, var beatmapIds) in beatmapIdsByBeatmapSetId) {
            Console.WriteLine(Strings.BeatmapUserMap_DoBeatmapUserMap_Fetching_beatmapset__0___1____2_, ++i, beatmapIdsByBeatmapSetId.Count, beatmapsetId);
            var beatmapset = client.GetBeatmapSetInfo(beatmapsetId);

            try {
                BuildBeatmapUserIdsForBeatmapSet(beatmapsetId, beatmapIds, beatmapset, result);
            }
            catch (InvalidDataException) {
                beatmapset = client.GetBeatmapSetInfo(beatmapsetId, true);
                BuildBeatmapUserIdsForBeatmapSet(beatmapsetId, beatmapIds, beatmapset, result);
            }
        }

        return result;
    }

    private static void BuildBeatmapUserIdsForBeatmapSet(int beatmapsetId, List<int> beatmapIds, JsonElement beatmapset, Dictionary<int, int> result) {
        if (!beatmapset.TryGetProperty("beatmaps", out var beatmapsProperty) || beatmapsProperty.ValueKind != JsonValueKind.Array) {
            throw new InvalidDataException($"Beatmapset {beatmapsetId} does not contain a valid beatmaps array in the osu! API response.");
        }

        foreach (int beatmapId in beatmapIds) {
            var beatmap = beatmapsProperty
                .EnumerateArray()
                .FirstOrDefault(b => b.TryGetProperty("id", out var idProperty) && idProperty.GetInt32() == beatmapId);

            if (beatmap.ValueKind == JsonValueKind.Undefined) {
                throw new InvalidDataException($"Beatmap {beatmapId} was not found in beatmapset {beatmapsetId} from the osu! API response.");
            }

            if (!beatmap.TryGetProperty("user_id", out var userIdProperty) || userIdProperty.ValueKind == JsonValueKind.Null) {
                throw new InvalidDataException($"Beatmap {beatmapId} in beatmapset {beatmapsetId} does not contain a user_id in the osu! API response.");
            }

            result[beatmapId] = userIdProperty.GetInt32();
        }
    }

    private static int FindRequiredHeaderIndex(IReadOnlyList<string> headers, string columnName) {
        for (var i = 0; i < headers.Count; i++) {
            if (string.Equals(headers[i], columnName, StringComparison.OrdinalIgnoreCase)) {
                return i;
            }
        }

        throw new InvalidDataException($"metadata.csv is missing the required '{columnName}' column.");
    }
}