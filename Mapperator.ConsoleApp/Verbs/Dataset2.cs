using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Mapperator.ConsoleApp.Resources;
using Mapperator.ConsoleApp.Utils;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.IO.Decoding;
using Mapping_Tools_Core.BeatmapHelper.IO.Encoding;
using MediaInfo;
using Parquet.Serialization;
using SharpCompress.Archives;
using File = System.IO.File;

// ReSharper disable NotAccessedPositionalProperty.Local

namespace Mapperator.ConsoleApp.Verbs;

public static class Dataset2 {
    [Verb("dataset2", HelpText = "Extract a ML dataset from a folder with a bunch of .osz files.")]
    public class DatasetOptions2 {
        [Option('i', "input", Required = true, HelpText = "Folder with .osz files.")]
        public string? InputFolder { get; [UsedImplicitly] set; }

        [Option('o', "output", Required = true, HelpText = "Folder to output the dataset to.")]
        public string? OutputFolder { get; [UsedImplicitly] set; }

        [Option('t', "tags", Required = false, HelpText = "Path to .csv file with OMDB tags for each beatmap ID.")]
        public string? OmdbTags { get; [UsedImplicitly] set; }

        [Option('m', "override-metadata", Default = false, Required = false, HelpText = "Override the metadata file if it already exists.")]
        public bool OverrideMetadata { get; [UsedImplicitly] set; }

        [Option('b', "override-beatmaps", Default = false, Required = false, HelpText = "Override the beatmaps if they already exist.")]
        public bool OverrideBeatmaps { get; [UsedImplicitly] set; }

        [Option('a', "override-audio", Default = false, Required = false, HelpText = "Override the audio files if they already exist.")]
        public bool OverrideAudio { get; [UsedImplicitly] set; }

        [Option('v', "validation-data", Required = false, HelpText = "Path to .txt file with all beatmap set IDs that you need.")]
        public string? ValidationData { get; [UsedImplicitly] set; }

        [Option('x', "validate", Default = false, Required = false, HelpText = "Validate the dataset. Makes sure each beatmap in the data folder has a corresponding metadata entry and vice versa.")]
        public bool Validate { get; [UsedImplicitly] set; }

        [Option('f', "set-id-from-file-name", Default = false, Required = false, HelpText = "Get the beatmap set ID from the .osz file name instead of the .osu file. Helpful for old maps where the set ID is not stored in the .osu file.")]
        public bool GetSetIdFromFileName { get; [UsedImplicitly] set; }

        [Option('r', "require-ranked", Default = false, Required = false, HelpText = "Require the beatmap set to be ranked. If false, will include unranked beatmaps.")]
        public bool RequireRanked { get; [UsedImplicitly] set; }

        [Option('c', "validate-checksums", Default = false, Required = false, HelpText = "Whether to check checksums of beatmaps against the online database. If false, will skip checksum validation.")]
        public bool ValidateChecksums { get; [UsedImplicitly] set; }

        [Option('d', "min-drain-time", Required = false, HelpText = "Minimum drain time in seconds for the beatmap to be included in the dataset.")]
        public int? MinDrainTime { get; [UsedImplicitly] set; } = null;

        [Option('p', "min-play-count", Required = false, HelpText = "Minimum play count for the beatmap to be included in the dataset.")]
        public int? MinPlayCount { get; [UsedImplicitly] set; } = null;

        [Option('s', "max-map-set-count", Required = false, HelpText = "Maximum number of beatmaps in a set to be included in the dataset. If null, no limit is applied.")]
        public int? MaxMapSetCount { get; [UsedImplicitly] set; } = null;

        [Option('e', "delete-on-issues", Default = false, Required = false, HelpText = "If true, delete the beatmap set folder if there are any issues with it.")]
        public bool DeleteOnIssues { get; [UsedImplicitly] set; } = false;

        [Option('n', "min-percentage-of-song-mapped", Required = false, HelpText = "Minimum percentage of the song that must be mapped for the beatmap to be included in the dataset.")]
        public int? MinPercentageOfSongMapped { get; [UsedImplicitly] set; } = null;

        [Option('l', "max-star-rating", Required = false, HelpText = "Maximum star rating for the beatmap to be included in the dataset.")]
        public float? MaxStarRating { get; [UsedImplicitly] set; } = null;
    }

    public static int DoDataExtraction2(DatasetOptions2 args) {
        if (args.InputFolder is null) throw new ArgumentNullException(nameof(args));
        if (args.OutputFolder is null) throw new ArgumentNullException(nameof(args));

        if (!Directory.Exists(args.OutputFolder)) {
            Directory.CreateDirectory(args.OutputFolder);
        }

        Dictionary<int, List<string>>? omdbTags = null;
        if (args.OmdbTags is not null) {
            if (!File.Exists(args.OmdbTags)) throw new FileNotFoundException(Strings.Dataset_DoDataExtraction2_OMDB_tags_file_not_found, args.OmdbTags);
            omdbTags = LoadOmdbTags(args.OmdbTags);
        }

        // Get OAuth token
        string token = GetAccessToken(ConfigManager.Config.ClientId, ConfigManager.Config.ClientSecret).Result;
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Dataset file structure:
        // OutputFolder
        // ├── metadata.parquet
        // ├── data
        // │   ├── 1 Kenji Ninuma - DISCO PRINCE
        // │   │   ├── 20.mp3
        // │   │   ├── Kenji Ninuma - DISCOPRINCE (peppy) [Normal].osu
        // │   ├── 3 Ni-Ni - 1,2,3,4, 007 [Wipeout Series]
        // │   │   ├── 1,2,3,4, 007 (Speed Pop Mix).mp3
        // │   │   ├── Ni-Ni - 1,2,3,4, 007 [Wipeout Series] (MCXD) [-Breezin-].osu
        // │   │   ├── Ni-Ni - 1,2,3,4, 007 [Wipeout Series] (MCXD) [-Crusin-].osu
        // │   │   ├── Ni-Ni - 1,2,3,4, 007 [Wipeout Series] (MCXD) [-Hardrock-].osu
        // │   │   ├── Ni-Ni - 1,2,3,4, 007 [Wipeout Series] (MCXD) [-Sweatin-].osu
        // ...

        const string dataFolder = "data";
        var beatmapDecoder = new OsuBeatmapDecoder();
        var beatmapEncoder = new OsuBeatmapEncoder();

        // Cache online beatmap set information
        var onlineCache = new Dictionary<int, JsonElement>();
        // Load from .jsonl file if it exists
        const string onlineCachePath = "online_cache.jsonl";
        if (File.Exists(onlineCachePath)) {
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Loading_online_cache___);
            foreach (string line in File.ReadLines(onlineCachePath)) {
                // Parse the JSON object
                var json = JsonSerializer.Deserialize<JsonElement>(line);
                int id = json.GetProperty("id").GetInt32();
                onlineCache[id] = json;
            }
        } else {
            // Create the file if it doesn't exist
            File.Create(onlineCachePath).Dispose();
        }

        // Make sure we don't exceed the API rate limit
        const int rateLimitInterval = 120;  // Milliseconds
        var lastApiCallTime = DateTime.MinValue;

        const int metadataCheckpointInterval = 100;  // Number of metadata entries to write before checkpointing
        int metadataCounter = 0;
        var allMetadata = new Dictionary<int, BeatmapMetadata>();
        var allSets = new HashSet<int>();
        var setsWithIssues = new HashSet<int>();
        var setsWithMismatchedChecksums = new HashSet<int>();
        var setsSkipped = new HashSet<string>();

        // If there is already a metadata file, load it
        string metadataPath = Path.Combine(args.OutputFolder, "metadata.parquet");
        if (File.Exists(metadataPath)) {
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Loading_existing_metadata___);
            var data = ParquetSerializer.DeserializeAsync<BeatmapMetadata>(metadataPath).Result;
            foreach (var beatmapMetadata in data) {
                allMetadata[beatmapMetadata.Id] = beatmapMetadata;
                allSets.Add(beatmapMetadata.BeatmapSetId);
            }
        }

        // Save sets with issues
        string setsWithIssuesPath = Path.Combine(args.OutputFolder, "sets_with_issues.txt");
        if (File.Exists(setsWithIssuesPath)) {
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Loading_existing_sets_with_issues___);
            foreach (string line in File.ReadLines(setsWithIssuesPath)) {
                setsWithIssues.Add(int.Parse(line));
            }
        }

        // Save sets with mismatched checksums
        string setsWithMismatchedChecksumsPath = Path.Combine(args.OutputFolder, "sets_with_mismatched_checksums.txt");
        if (File.Exists(setsWithMismatchedChecksumsPath)) {
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Loading_existing_sets_with_mismatched_checksums___);
            foreach (string line in File.ReadLines(setsWithMismatchedChecksumsPath)) {
                setsWithMismatchedChecksums.Add(int.Parse(line));
            }
        }

        // Save sets skipped
        string setsSkippedPath = Path.Combine(args.OutputFolder, "sets_skipped.txt");
        if (File.Exists(setsSkippedPath)) {
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Loading_existing_sets_skipped___);
            foreach (string line in File.ReadLines(setsSkippedPath)) {
                setsSkipped.Add(line);
            }
        }

        Console.WriteLine(Strings.Dataset_DoDataExtraction_Finding_beatmap_sets___);

        foreach ((string fullName, var oszFile) in FindOszFiles(args.InputFolder)) {
            if (args.MaxMapSetCount.HasValue && allSets.Count >= args.MaxMapSetCount.Value) {
                Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Maximum_beatmap_set_count_of__0__reached__Stopping_extraction_, args.MaxMapSetCount);
                break;
            }

            try {
                Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Mapset___0____1_, allSets.Count + 1, fullName);

                var issues = false;

                // Get the beatmap set ID
                int setId;
                if (args.GetSetIdFromFileName) {
                    // Extract the set ID from the file name
                    if (!int.TryParse(Path.GetFileNameWithoutExtension(fullName).Split(' ')[0], out setId)) {
                        Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Invalid__osz_file_name_format___0___Expected_format___SetID___Artist_____Title__osz, fullName);
                        if (setsSkipped.Add(fullName))
                            File.AppendAllLines(setsSkippedPath, [fullName]);
                        continue;
                    }
                }
                else {
                    // Read the 'BeatmapSetID' key in the first .osu entry in the .osz file to get the set ID
                    var firstOsuEntry = oszFile.Entries.FirstOrDefault(e => Path.GetExtension(e.FullName).Equals(".osu", StringComparison.OrdinalIgnoreCase));
                    if (firstOsuEntry is null) {
                        Console.WriteLine(Strings.Dataset2_DoDataExtraction2_No__osu_file_found_in__0___Skipping_, fullName);
                        if (setsSkipped.Add(fullName))
                            File.AppendAllLines(setsSkippedPath, [fullName]);
                        continue;
                    }

                    string? beatmapSetIdLine = GetBeatmapKeyValue(firstOsuEntry, "BeatmapSetID");
                    if (beatmapSetIdLine is null || !int.TryParse(beatmapSetIdLine, out setId)) {
                        Console.WriteLine(Strings.Dataset2_DoDataExtraction2_BeatmapSetID_not_found_in__0___Skipping__1__, firstOsuEntry.FullName, fullName);
                        if (setsSkipped.Add(fullName))
                            File.AppendAllLines(setsSkippedPath, [fullName]);
                        continue;
                    }
                }

                // Get the online information for the beatmap set
                if (!onlineCache.TryGetValue(setId, out var beatmapsetInfo)) {
                    // Ensure at least 120 ms has passed since the last API call
                    double timeSinceLastCall = (DateTime.Now - lastApiCallTime).TotalMilliseconds;
                    if (timeSinceLastCall < rateLimitInterval)
                        Thread.Sleep(rateLimitInterval - (int)timeSinceLastCall);
                    lastApiCallTime = DateTime.Now;

                    // Make an API request to get the beatmap set information
                    try {
                        beatmapsetInfo = GetBeatmapSetInfo(client, setId).Result;
                    }
                    catch (Exception e) {
                        Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Failed_to_get_beatmap_set_info_for_ID__0____1_, setId, e.Message);
                        if (setsSkipped.Add(fullName))
                            File.AppendAllLines(setsSkippedPath, [fullName]);
                        continue;
                    }

                    // Cache the information by appending to the online cache file
                    onlineCache[setId] = beatmapsetInfo;
                    File.AppendAllLines(onlineCachePath, [beatmapsetInfo.GetRawText()]);
                }

                bool filterBeatmap(JsonElement beatmapInfo) {
                    if (args.RequireRanked && beatmapInfo.GetProperty("status").GetString() != "ranked" && beatmapInfo.GetProperty("status").GetString() != "approved") {
                        return false;
                    }

                    if (args.MinDrainTime.HasValue && beatmapInfo.GetProperty("hit_length").GetInt32() < args.MinDrainTime.Value) {
                        return false;
                    }

                    if (args.MinPlayCount.HasValue && beatmapInfo.GetProperty("playcount").GetInt32() < args.MinPlayCount.Value) {
                        return false;
                    }

                    if (args.MaxStarRating.HasValue && beatmapInfo.GetProperty("difficulty_rating").GetSingle() > args.MaxStarRating.Value) {
                        return false;
                    }

                    return true;
                }

                // Filter beatmaps based on the provided criteria
                var includedIds = beatmapsetInfo.GetProperty("beatmaps").EnumerateArray()
                    .Where(filterBeatmap)
                    .Select(b => b.GetProperty("id").GetInt32())
                    .ToHashSet();

                // Skip if the beatmap set has no valid beatmaps
                if (includedIds.Count == 0) {
                    Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Skipping__0__because_it_has_no_beatmaps_matching_the_criteria_, fullName);
                    continue;
                }

                // Skip the whole beatmap set if it's already processed and we are not overriding
                if (args is { OverrideMetadata: false, OverrideBeatmaps: false, OverrideAudio: false } && includedIds.All(id => allMetadata.ContainsKey(id))) {
                    continue;
                }

                int beatmapSetId = beatmapsetInfo.GetProperty("id").GetInt32();
                string artist = beatmapsetInfo.GetProperty("artist").GetString()!;
                string title = beatmapsetInfo.GetProperty("title").GetString()!;

                var outputSetFolder = $"{beatmapSetId} {artist} - {title}";
                outputSetFolder = TrimBeatmapSetFolderName(outputSetFolder);
                string outputSetPath = Path.Combine(args.OutputFolder, dataFolder, outputSetFolder);

                // Make sure the folder exists
                Directory.CreateDirectory(outputSetPath);

                // Get the ranked MD5 hashes
                Dictionary<string, JsonElement> md5Hashes = new();
                foreach (var beatmapInfo in beatmapsetInfo.GetProperty("beatmaps").EnumerateArray()) {
                    md5Hashes.Add(beatmapInfo.GetProperty("checksum").GetString()!, beatmapInfo);
                }

                // Get the beatmap IDs
                Dictionary<int, JsonElement> beatmapIds = new();
                foreach (var beatmapInfo in beatmapsetInfo.GetProperty("beatmaps").EnumerateArray()) {
                    beatmapIds.Add(beatmapInfo.GetProperty("id").GetInt32(), beatmapInfo);
                }

                foreach (var entry in oszFile.Entries) {
                    if (!Path.GetExtension(entry.FullName).Equals(".osu", StringComparison.OrdinalIgnoreCase)) continue;

                    // Try to find the online beatmap info entry for this particular file
                    using var hashStream = entry.Open();
                    string checksum = hashStream.ComputeMD5Hash();

                    if (!md5Hashes.TryGetValue(checksum, out var beatmapInfo)) {
                        if (args.ValidateChecksums) {
                            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Skipping__0__because_checksum_does_not_match_, entry.FullName);
                            if (setsWithMismatchedChecksums.Add(beatmapSetId))
                                File.AppendAllLines(setsWithMismatchedChecksumsPath, [beatmapSetId.ToString()]);
                            continue;
                        }

                        // Try to match by beatmap ID if checksums are not validated
                        string? beatmapIdLine = GetBeatmapKeyValue(entry, "BeatmapID");
                        if (beatmapIdLine is null || !int.TryParse(beatmapIdLine, out int beatmapId2) || !beatmapIds.TryGetValue(beatmapId2, out beatmapInfo)) {
                            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Skipping__0__because_checksum_does_not_match_and_BeatmapID_is_not_found_, entry.FullName);
                            if (setsWithMismatchedChecksums.Add(beatmapSetId))
                                File.AppendAllLines(setsWithMismatchedChecksumsPath, [beatmapSetId.ToString()]);
                            continue;
                        }
                    }

                    int beatmapId = beatmapInfo.GetProperty("id").GetInt32();

                    if (!includedIds.Contains(beatmapId)) {
                        Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Skipping__0__because_it_does_not_match_the_criteria_, entry.FullName);
                        continue;
                    }

                    if (allMetadata.ContainsKey(beatmapId) && args is { OverrideMetadata: false, OverrideBeatmaps: false, OverrideAudio: false }) {
                        continue;
                    }

                    Beatmap beatmap;
                    try {
                        using var stream = entry.Open();
                        using var reader = new StreamReader(stream);
                        string content = reader.ReadToEnd();
                        beatmap = beatmapDecoder.Decode(content);
                    }
                    catch (Exception e) {
                        Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Failed_to_decode_beatmap__0____1_, entry.FullName, e.Message);
                        issues = true;
                        continue;
                    }

                    if (string.IsNullOrEmpty(beatmap.General.AudioFilename)) {
                        Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Audio_filename_is_missing_in_beatmap__0___Skipping_this_beatmap_, entry.FullName);
                        issues = true;
                        continue;
                    }

                    string actualAudioFilename = WindowsZipFilenameStrip(FindAudioFile(oszFile, beatmap.General.AudioFilename)?.Name ?? beatmap.General.AudioFilename);
                    string actualBeatmapFilename = WindowsZipFilenameStrip(entry.Name);

                    // Filter on percentage of song mapped
                    if (args.MinPercentageOfSongMapped.HasValue) {
                        var audioEntry = oszFile.GetEntry(actualAudioFilename);
                        if (audioEntry is not null) {
                            try {
                                using var memoryStream = new MemoryStream();
                                using (var audioStream = audioEntry.Open()) audioStream.CopyTo(memoryStream);
                                double duration = GetMediaInfoFromStream(memoryStream, actualAudioFilename).TotalSeconds;
                                int drainTime = beatmapInfo.GetProperty("hit_length").GetInt32();
                                double percentageMapped = drainTime / duration * 100;
                                if (percentageMapped < args.MinPercentageOfSongMapped.Value) {
                                    Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Skipping__0__because_it_is_mapped_only__1_F2___of_the_song_, entry.FullName,
                                        percentageMapped);
                                    continue;
                                }
                            }
                            catch (Exception ex) {
                                Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Error_reading_audio_duration_for__0____1_, entry.FullName, ex.Message);
                                issues = true;
                            }
                        }
                    }

                    // Copy the beatmap file to the output folder
                    string outputOsuFile = Path.Combine(outputSetPath, actualBeatmapFilename);
                    if (!File.Exists(outputOsuFile) || args.OverrideBeatmaps) {
                        // Upgrade file format to v14
                        beatmap.UpgradeBeatmapVersion();
                        // Remove storyboard
                        ClearStoryboard(beatmap.Storyboard);
                        // Ensure beatmap ID and beatmap set ID matches the database
                        beatmap.Metadata.BeatmapId = beatmapId;
                        beatmap.Metadata.BeatmapSetId = beatmapSetId;
                        // Ensure AudioFilename is correct
                        beatmap.General.AudioFilename = actualAudioFilename;

                        File.WriteAllText(outputOsuFile, beatmapEncoder.Encode(beatmap));
                    }

                    // Copy the audio file to the output folder
                    string outputAudioFile = Path.Combine(outputSetPath, actualAudioFilename);
                    if (!File.Exists(outputAudioFile) || args.OverrideAudio) {
                        // Find the audio file in the .osz file
                        var audioEntry = oszFile.GetEntry(actualAudioFilename);
                        if (audioEntry is null) {
                            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Audio_file__0__not_found_in__1_, actualAudioFilename, fullName);
                            issues = true;
                        }
                        else {
                            using var audioStream = audioEntry.Open();
                            using var audioFile = File.Create(outputAudioFile);
                            audioStream.CopyTo(audioFile);
                        }
                    }

                    // Create dataset entry
                    if (!allMetadata.ContainsKey(beatmapId) || args.OverrideMetadata) {
                        var metadata = ParseBeatmapMetadata(beatmapsetInfo, beatmapInfo);

                        metadata.AudioFile = actualAudioFilename;
                        metadata.BeatmapSetFolder = outputSetFolder;
                        metadata.BeatmapFile = actualBeatmapFilename;

                        // Calculate difficulty values
                        metadata.StarRating = CalculateDifficultyValues(outputOsuFile);

                        // Add OMDB tags if available
                        if (omdbTags is not null && omdbTags.TryGetValue(beatmapId, out var tags)) {
                            metadata.OmdbTags = tags;
                        }

                        // Append to the parquet file
                        if (metadataCounter++ >= metadataCheckpointInterval) {
                            ParquetSerializer.SerializeAsync(allMetadata.Values, metadataPath);
                            metadataCounter = 0;
                        }

                        allMetadata[metadata.Id] = metadata;
                        allSets.Add(metadata.BeatmapSetId);
                    }
                }

                if (args.DeleteOnIssues && issues) {
                    Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Deleting_beatmap_set_folder__0__due_to_issues_, outputSetPath);
                    Directory.Delete(outputSetPath, true);
                    // Remove the metadata entries
                    foreach (int beatmapId in includedIds) {
                        allMetadata.Remove(beatmapId);
                    }

                    allSets.Remove(beatmapSetId);
                    // Report that we skipped this beatmap set
                    if (setsSkipped.Add(fullName))
                        File.AppendAllLines(setsSkippedPath, [fullName]);
                    issues = false; // Prevent reporting issues
                }

                // Remove the beatmapset folder if it's empty
                if (Directory.Exists(outputSetPath) && Directory.GetFiles(outputSetPath).Length == 0)
                    Directory.Delete(outputSetPath);

                // Report any beatmap sets with issues
                if (issues && setsWithIssues.Add(beatmapSetId)) {
                    // Append to the sets with issues file
                    File.AppendAllLines(setsWithIssuesPath, [beatmapSetId.ToString()]);
                }
            } catch (Exception e) {
                Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Unexpected_error_processing_beatmap_set__0____1___2_, fullName, e.Message, e.StackTrace);
                // If there was an error processing the beatmap set, skip it
                if (setsSkipped.Add(fullName))
                    File.AppendAllLines(setsSkippedPath, [fullName]);
            }
        }

        // Write metadata to a parquet file
        ParquetSerializer.SerializeAsync(allMetadata.Values, metadataPath);

        // Report any beatmap sets with issues
        if (setsWithIssues.Count > 0) {
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Beatmap_sets_with_issues_);
            foreach (int setId in setsWithIssues) {
                Console.WriteLine(setId);
            }
        }

        // Report any beatmap sets with mismatched checksums
        if (setsWithMismatchedChecksums.Count > 0) {
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Beatmap_sets_with_mismatched_checksums_);
            foreach (int setId in setsWithMismatchedChecksums) {
                Console.WriteLine(setId);
            }
        }

        // Report any skipped beatmap sets
        if (setsSkipped.Count > 0) {
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Beatmap_sets_skipped_due_to_issues_);
            foreach (string setId in setsSkipped) {
                Console.WriteLine(setId);
            }
        }

        // Report any missing ranked beatmaps
        if (args.ValidationData is not null) {
            var validationData = File.ReadLines(args.ValidationData).Select(int.Parse).ToHashSet();
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Missing_ranked_beatmap_sets_);
            foreach (int beatmapSetId in validationData.Except(allSets)) {
                Console.WriteLine(beatmapSetId);
            }
        }

        // Check if each beatmap in the data folder has a corresponding metadata entry
        if (args.Validate) {
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Checking_for_missing_metadata___);
            var allMetadataBeatmapFiles = allMetadata.Values.Select(x => x.BeatmapFile).ToHashSet();
            foreach (string osuFile in Directory.EnumerateFiles(Path.Combine(args.OutputFolder, dataFolder), "*.osu", SearchOption.AllDirectories)) {
                string fileName = Path.GetFileName(osuFile);
                if (!allMetadataBeatmapFiles.Contains(fileName)) {
                    Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Missing_metadata_for__0_, fileName);
                }
            }

            // Check if each metadata entry has a corresponding beatmap in the data folder
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Checking_for_missing_data___);
            foreach (var metadata in allMetadata.Values) {
                string osuFile = Path.Combine(args.OutputFolder, dataFolder, metadata.BeatmapSetFolder, metadata.BeatmapFile);
                if (!File.Exists(osuFile)) {
                    Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Missing_beatmap_for__0_, osuFile);
                    allMetadata.Remove(metadata.Id);
                }

                string audioFile = Path.Combine(args.OutputFolder, dataFolder, metadata.BeatmapSetFolder, metadata.AudioFile);
                if (!File.Exists(audioFile)) {
                    Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Missing_audio_file_for__0_, audioFile);
                    allMetadata.Remove(metadata.Id);
                }
            }

            // Write metadata to a parquet file
            ParquetSerializer.SerializeAsync(allMetadata.Values, metadataPath);
            allSets = allMetadata.Values.Select(x => x.BeatmapSetId).ToHashSet();
        }

        // Count total file size and duration in the dataset
        Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Counting_total_file_size_and_duration_in_the_dataset___);
        long totalSize = 0;
        var totalTime = TimeSpan.Zero;

        // Search for audio files (.mp3 and .ogg) and aggregate duration
        var audioFiles = Directory.EnumerateFiles(args.OutputFolder, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase));

        foreach (string audioFile in audioFiles) {
            try {
                totalTime += GetMediaFileDuration(audioFile);
            } catch (Exception e) {
                Console.WriteLine(Strings.Dataset2_DoDataExtraction2_, e.Message, audioFile);
            }
        }

        // Search for files and aggregate size
        var osuFiles = Directory.EnumerateFiles(args.OutputFolder, "*.*", SearchOption.AllDirectories);

        foreach (string osuFile in osuFiles) {
            var info = new FileInfo(osuFile);
            totalSize += info.Length;
        }

        Console.WriteLine();
        Console.WriteLine(Strings.Count_DoDataCount_Total_file_size___0__MB, totalSize / 1024 / 1024);
        Console.WriteLine(Strings.Count_DoDataCount_Total_duration___0_, totalTime);

        // Count total beatmap sets and beatmaps
        int totalBeatmapSets = allSets.Count;
        int totalBeatmaps = allMetadata.Count;

        Console.WriteLine();
        Console.WriteLine(Strings.Count_DoDataCount_Total_beatmap_sets___0_, totalBeatmapSets);
        Console.WriteLine(Strings.Count_DoDataCount_Total_beatmaps___0_, totalBeatmaps);

        return 0;
    }

    private static TimeSpan GetMediaFileDuration(string filePath)
    {
        var mi = new MediaInfo.MediaInfo();

        try
        {
            mi.Open(filePath);

            string durationString = mi.Get(StreamKind.General, 0, "Duration");

            if (!string.IsNullOrEmpty(durationString) && int.TryParse(durationString, out int durationMs))
                return TimeSpan.FromMilliseconds(durationMs);
            else
                throw new InvalidOperationException($"Could not retrieve duration for file: {filePath}");
        }
        finally
        {
            mi.Close();
        }
    }

    private static TimeSpan GetMediaInfoFromStream(MemoryStream memoryStream, string fileName)
    {
        using var mi = new MediaInfo.MediaInfo();

        // Reset the stream's position to the beginning
        memoryStream.Position = 0;

        mi.OpenBufferInit(memoryStream.Length, 0);

        const int chunkSize = 64 * 1024; // 64KB
        var buffer = new byte[chunkSize];
        int read;
        while ((read = memoryStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                mi.OpenBufferContinue(handle.AddrOfPinnedObject(), read);
            }
            finally
            {
                handle.Free();
            }
        }

        mi.OpenBufferFinalize();

        // Now you can get the properties as usual
        string durationString = mi.Get(StreamKind.General, 0, "Duration");

        if (string.IsNullOrEmpty(durationString) || !int.TryParse(durationString, out int durationMs))
            throw new InvalidOperationException($"Could not retrieve duration for file: {fileName}");

        return TimeSpan.FromMilliseconds(durationMs);
    }

    private static string? GetBeatmapKeyValue(ZipArchiveEntry entry, string key) {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line) {
            if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase)) {
                return line.Split(':')[1].Trim();
            }
        }
        return null;
    }

    private static ZipArchiveEntry? FindAudioFile(ZipArchive oszFile, string audioFilename) {
        var result = oszFile.GetEntry(audioFilename);
        return result ?? oszFile.Entries.FirstOrDefault(entry => WindowsZipFilenameStrip(entry.Name).Equals(audioFilename, StringComparison.OrdinalIgnoreCase));
    }

    private static string TrimBeatmapSetFolderName(string folderName) {
        folderName = WindowsPathStrip(folderName);

        const int maxPathLength = 248;
        int datasetDirLength = "C:/datasets/MMRS12345/".Length;
        int charsOver = datasetDirLength + folderName.Length * 2 - maxPathLength;

        if (charsOver <= 0) return folderName.Trim();

        if (charsOver < folderName.Length - 1)
            folderName = folderName.Remove(folderName.Length - charsOver);
        else
            throw new PathTooLongException();

        return folderName.Trim();
    }

    private static string WindowsPathStrip(string entry)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            entry = entry.Replace(c.ToString(), string.Empty);
        entry = entry.Replace(".", string.Empty);
        return entry;
    }

    public static string WindowsZipFilenameStrip(string entry)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            entry = entry.Replace(c.ToString(), string.Empty);
        return entry.Trim();
    }

    private static List<float> CalculateDifficultyValues(string beatmapPath) {
        var beatmap = DifficultyCalculatorUtils.GetBeatmap(beatmapPath);
        var difficultyCalculator = DifficultyCalculatorUtils.CreateDifficultyCalculator(beatmap);
        float[] speeds = [0.5f, 0.75f, 1f, 1.25f, 1.5f, 1.75f, 2f];
        var starRating = new List<float>();

        foreach (float speed in speeds) {
            var mod = DifficultyCalculatorUtils.GetRateAdjust(beatmap, speed);
            starRating.Add((float)difficultyCalculator.Calculate([mod]).StarRating);
        }

        return starRating;
    }

    private static string ToLowercaseHex(this byte[] bytes)
    {
        // Convert.ToHexString is upper-case, so we are doing this ourselves

        return string.Create(bytes.Length * 2, bytes, (span, b) => {
            for (var i = 0; i < b.Length; i++)
                _ = b[i].TryFormat(span[(i * 2)..], out _, "x2");
        });
    }

    public static string ComputeMD5Hash(this Stream stream) => MD5.HashData(stream).ToLowercaseHex();

    private static Dictionary<int, List<string>> LoadOmdbTags(string omdbTagsPath) {
        // Example file contents:
        // 1,tag1
        // 1,tag2
        // 2,tag3
        var omdbTags = new Dictionary<int, List<string>>();
        foreach (string line in File.ReadLines(omdbTagsPath)) {
            string[] split = line.Split(',');
            if (split.Length < 2) continue;
            if (!int.TryParse(split[0], out int id)) continue;
            string tag = split[1];
            if (!omdbTags.ContainsKey(id)) omdbTags[id] = new List<string>();
            omdbTags[id].Add(tag);
        }
        return omdbTags;
    }

    private static IEnumerable<(string, ZipArchive)> FindOszFiles(string rootDirectory)
    {
        var validArchiveExtensions = new[] { ".zip", ".rar", ".7z", ".tar", ".tar.gz", ".tar.bz2", ".tar.lz", ".tar.xz" };

        if (validArchiveExtensions.Contains(Path.GetExtension(rootDirectory), StringComparer.OrdinalIgnoreCase)) {
            foreach (var oszFile in SearchArchiveForOszFiles(rootDirectory))
                yield return oszFile;
            yield break;
        }

        // Check all files and directories in the root directory
        foreach (string entry in Directory.EnumerateFileSystemEntries(rootDirectory, "*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(entry).Equals(".osz", StringComparison.OrdinalIgnoreCase))
            {
                // Yield .osz files directly found in the directory tree
                // Open the .osz file as a stream and treat it as a zip archive
                using var oszArchive = ZipFile.OpenRead(entry);
                yield return (entry, oszArchive);
            }
            else if (validArchiveExtensions.Contains(Path.GetExtension(entry), StringComparer.OrdinalIgnoreCase))
            {
                // Process .zip files as folders
                foreach (var oszFile in SearchArchiveForOszFiles(entry))
                    yield return oszFile;
            }
        }
    }

    private static IEnumerable<(string, ZipArchive)> SearchArchiveForOszFiles(string archivePath) {
        using Stream stream = File.OpenRead(archivePath);
        using var archive = ArchiveFactory.Open(stream);
        foreach (var entry in archive.Entries) {
            if (!Path.GetExtension(entry.Key!).Equals(".osz", StringComparison.OrdinalIgnoreCase)) continue;

            ZipArchive oszArchive;
            try {
                // Open the .osz file as a stream and treat it as a zip archive
                using var oszStream = entry.OpenEntryStream();
                oszArchive = new ZipArchive(oszStream, ZipArchiveMode.Read);
            } catch (Exception e) {
                Console.WriteLine(Strings.Dataset2_SearchArchiveForOszFiles_Error_opening__osz_file__0____1_, entry.Key, e.Message);
                continue;
            }

            yield return (entry.Key!, oszArchive);
            oszArchive.Dispose();
        }
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

    private const string TokenUrl = "https://osu.ppy.sh/oauth/token";

    private static async Task<string> GetAccessToken(string clientId, string clientSecret) {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var requestBody = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "public")
        ]);

        var response = await client.PostAsync(TokenUrl, requestBody);
        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);
            return tokenResponse!.AccessToken;
        }

        Console.WriteLine(Strings.Dataset2_GetAccessToken_Failed_to_get_osu__API_access_token__Make_sure_your_ClientId_and_ClientSecret_are_correctly_set_in_config_json_);
        throw new Exception($"Error: {response.StatusCode}");
    }

    public class TokenResponse {
        [JsonPropertyName("token_type")] public string TokenType { get; set; } = null!;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = null!;
    }

    private const string ApiUrl = "https://osu.ppy.sh/api/v2/beatmapsets";

    private static async Task<JsonElement> GetBeatmapSetInfo(HttpClient client, int setId) {
        const int maxRetries = 10;
        var retries = 0;
        HttpResponseMessage? response = null;
        while (retries++ < maxRetries) {
            response = await client.GetAsync($"{ApiUrl}/{setId}");

            if (response.IsSuccessStatusCode) {
                break;
            }

            if (retries >= maxRetries) {
                throw new Exception($"Failed to fetch beatmapset info: {response.StatusCode}");
            }

            switch (response.StatusCode) {
                case System.Net.HttpStatusCode.TooManyRequests:
                    Console.WriteLine(Strings.Dataset2_GetBeatmapsetInfo_Rate_limited__waiting_10_seconds___);
                    Thread.Sleep(10000);
                    break;
                case System.Net.HttpStatusCode.GatewayTimeout:
                case System.Net.HttpStatusCode.RequestTimeout:
                    Console.WriteLine(Strings.Dataset2_GetBeatmapsetInfo_Timed_out__waiting_10_seconds___);
                    Thread.Sleep(10000);
                    break;
                default: {
                    throw new Exception($"Failed to fetch beatmapset info: {response.StatusCode}");
                }
            }
        }

        string jsonResponse = await response!.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(jsonResponse);
    }

    private static BeatmapMetadata ParseBeatmapMetadata(JsonElement beatmapset, JsonElement beatmap) {
        return new BeatmapMetadata {
            // Beatmapset
            Artist = beatmapset.GetProperty("artist").GetString()!,
            ArtistUnicode = beatmapset.GetProperty("artist_unicode").GetString()!,
            Creator = beatmapset.GetProperty("creator").GetString()!,
            FavouriteCount = beatmapset.GetProperty("favourite_count").GetInt32(),
            BeatmapSetId = beatmapset.GetProperty("id").GetInt32(),
            Nsfw = beatmapset.GetProperty("nsfw").GetBoolean(),
            Offset = beatmapset.GetProperty("offset").GetInt32(),
            BeatmapSetPlayCount = beatmapset.GetProperty("play_count").GetInt32(),
            Source = beatmapset.GetProperty("source").GetString()!,
            BeatmapSetStatus = beatmapset.GetProperty("status").GetString()!,
            Spotlight = beatmapset.GetProperty("spotlight").GetBoolean(),
            Title = beatmapset.GetProperty("title").GetString()!,
            TitleUnicode = beatmapset.GetProperty("title_unicode").GetString()!,
            BeatmapSetUserId = beatmapset.GetProperty("user_id").GetInt32(),
            Video = beatmapset.GetProperty("video").GetBoolean(),
            Description = beatmapset.GetProperty("description").GetProperty("description").GetString()!,
            GenreId = beatmapset.GetProperty("genre").TryGetProperty("id", out var genreIdElem) && genreIdElem.ValueKind != JsonValueKind.Null ? genreIdElem.GetInt32() : null,
            GenreName = beatmapset.GetProperty("genre").GetProperty("name").GetString()!,
            LanguageId = beatmapset.GetProperty("language").TryGetProperty("id", out var languageIdElem) && languageIdElem.ValueKind != JsonValueKind.Null ? languageIdElem.GetInt32() : null,
            LanguageName = beatmapset.GetProperty("language").GetProperty("name").GetString()!,
            PackTags = beatmapset.GetProperty("pack_tags").EnumerateArray().Select(x => x.GetString()!).ToList(),
            Ratings = beatmapset.GetProperty("ratings").EnumerateArray().Select(x => x.GetInt32()).ToList(),

            // BeatmapsetExtended
            DownloadDisabled = beatmapset.GetProperty("availability").GetProperty("download_disabled").GetBoolean(),
            BeatmapSetBpm = beatmapset.GetProperty("bpm").GetSingle(),
            CanBeHyped = beatmapset.GetProperty("can_be_hyped").GetBoolean(),
            DiscussionLocked = beatmapset.GetProperty("discussion_locked").GetBoolean(),
            BeatmapSetIsScoreable = beatmapset.GetProperty("is_scoreable").GetBoolean(),
            BeatmapSetLastUpdated = beatmapset.GetProperty("last_updated").GetDateTime(),
            BeatmapSetRanked = beatmapset.GetProperty("ranked").GetInt32(),
            RankedDate = beatmapset.TryGetProperty("ranked_date", out var rankedDateElem) && rankedDateElem.ValueKind != JsonValueKind.Null && rankedDateElem.TryGetDateTime(out var rankedDate) ? rankedDate : null,
            Storyboard = beatmapset.GetProperty("storyboard").GetBoolean(),
            SubmittedDate = beatmapset.TryGetProperty("submitted_date", out var submittedDateElem) && submittedDateElem.ValueKind != JsonValueKind.Null && submittedDateElem.TryGetDateTime(out var submittedDate) ? submittedDate : null,
            Tags = beatmapset.GetProperty("tags").GetString()!,

            // Beatmap
            DifficultyRating = beatmap.GetProperty("difficulty_rating").GetSingle(),
            Id = beatmap.GetProperty("id").GetInt32(),
            Mode = beatmap.GetProperty("mode").GetString()!,
            Status = beatmap.GetProperty("status").GetString()!,
            TotalLength = beatmap.GetProperty("total_length").GetInt32(),
            UserId = beatmap.GetProperty("user_id").GetInt32(),
            Version = beatmap.GetProperty("version").GetString()!,
            Checksum = beatmap.TryGetProperty("checksum", out var checksumElem) && checksumElem.ValueKind != JsonValueKind.Null ? checksumElem.GetString() : null,
            MaxCombo = beatmap.TryGetProperty("max_combo", out var maxComboElem) && maxComboElem.ValueKind != JsonValueKind.Null ? maxComboElem.GetInt32() : null,
            Accuracy = beatmap.GetProperty("accuracy").GetSingle(),
            Ar = beatmap.GetProperty("ar").GetSingle(),
            Bpm = beatmap.TryGetProperty("bpm", out var bpmElem) && bpmElem.ValueKind != JsonValueKind.Null && bpmElem.TryGetSingle(out float bpm) ? bpm : null,
            CountCircles = beatmap.GetProperty("count_circles").GetInt32(),
            CountSliders = beatmap.GetProperty("count_sliders").GetInt32(),
            CountSpinners = beatmap.GetProperty("count_spinners").GetInt32(),
            Cs = beatmap.GetProperty("cs").GetSingle(),
            Drain = beatmap.GetProperty("drain").GetSingle(),
            HitLength = beatmap.GetProperty("hit_length").GetInt32(),
            IsScoreable = beatmap.GetProperty("is_scoreable").GetBoolean(),
            LastUpdated = beatmap.GetProperty("last_updated").GetDateTime(),
            ModeInt = beatmap.GetProperty("mode_int").GetInt32(),
            PassCount = beatmap.GetProperty("passcount").GetInt32(),
            PlayCount = beatmap.GetProperty("playcount").GetInt32(),
            Ranked = beatmap.GetProperty("ranked").GetInt32(),
            Owners = beatmap.TryGetProperty("owners", out var ownersElem) && ownersElem.ValueKind != JsonValueKind.Null
                ? ownersElem.EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).ToList()
                : [],
            TopTagIds = beatmap.TryGetProperty("top_tag_ids", out var topTagIdsElem) && topTagIdsElem.ValueKind != JsonValueKind.Null
                ? topTagIdsElem.EnumerateArray().Select(x => x.GetProperty("tag_id").GetInt32()).ToList()
                : [],
            TopTagCounts = beatmap.TryGetProperty("top_tag_ids", out var topTagCountsElem) && topTagCountsElem.ValueKind != JsonValueKind.Null
                ? topTagCountsElem.EnumerateArray().Select(x => x.GetProperty("count").GetInt32()).ToList()
                : [],
        };
    }

    private class BeatmapMetadata {
        // Beatmapset
        public string Artist { get; set; } = null!;
        public string ArtistUnicode { get; set; } = null!;
        public string Creator { get; set; } = null!;
        public int FavouriteCount { get; set; }
        public int BeatmapSetId { get; set; }
        public bool Nsfw { get; set; }
        public int Offset { get; set; }
        public int BeatmapSetPlayCount { get; set; }
        public string Source { get; set; } = null!;
        public string BeatmapSetStatus { get; set; } = null!;
        public bool Spotlight { get; set; }
        public string Title { get; set; } = null!;
        public string TitleUnicode { get; set; } = null!;
        public int BeatmapSetUserId { get; set; }
        public bool Video { get; set; }
        public string Description { get; set; } = null!;
        public int? GenreId { get; set; }
        public string GenreName { get; set; } = null!;
        public int? LanguageId { get; set; }
        public string LanguageName { get; set; } = null!;
        public List<string> PackTags { get; set; } = null!;
        public List<int>? Ratings { get; set; }

        // BeatmapsetExtended
        public bool DownloadDisabled { get; set; }
        public float BeatmapSetBpm { get; set; }
        public bool CanBeHyped { get; set; }
        public bool DiscussionLocked { get; set; }
        public bool BeatmapSetIsScoreable { get; set; }
        public DateTime BeatmapSetLastUpdated { get; set; }
        public int BeatmapSetRanked { get; set; }
        public DateTime? RankedDate { get; set; }
        public bool Storyboard { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string Tags { get; set; } = null!;

        // Beatmap
        public float DifficultyRating { get; set; }
        public int Id { get; set; }
        public string Mode { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int TotalLength { get; set; }
        public int UserId { get; set; }
        public string Version { get; set; } = null!;
        public string? Checksum { get; set; }
        public int? MaxCombo { get; set; }

        // BeatmapExtended
        public float Accuracy { get; set; }
        public float Ar { get; set; }
        public float? Bpm { get; set; }
        public int CountCircles { get; set; }
        public int CountSliders { get; set; }
        public int CountSpinners { get; set; }
        public float Cs { get; set; }
        public float Drain { get; set; }
        public int HitLength { get; set; }
        public bool IsScoreable { get; set; }
        public DateTime LastUpdated { get; set; }
        public int ModeInt { get; set; }
        public int PassCount { get; set; }
        public int PlayCount { get; set; }
        public int Ranked { get; set; }
        public List<int> Owners { get; set; } = [];
        public List<int> TopTagIds { get; set; } = [];
        public List<int> TopTagCounts { get; set; } = [];

        // Star ratings for various speeds
        public List<float> StarRating { get; set; } = [];

        // OMDB
        public List<string> OmdbTags { get; set; } = [];

        // Paths
        public string AudioFile { get; set; } = string.Empty;
        public string BeatmapSetFolder { get; set; } = string.Empty;
        public string BeatmapFile { get; set; } = string.Empty;
    }
}