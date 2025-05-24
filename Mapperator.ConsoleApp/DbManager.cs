using System;
using OsuParsers.Database.Objects;
using OsuParsers.Decoders;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mapperator.ConsoleApp.Exceptions;
using Mapperator.ConsoleApp.Resources;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
using OsuParsers.Database;
using OsuParsers.Enums;

namespace Mapperator.ConsoleApp {
    public static class DbManager {
        public static IEnumerable<DbBeatmap> GetCollection(string collectionName) {
            var osuDbPath = Path.Join(ConfigManager.Config.OsuPath, "osu!.db");
            var collectionPath = Path.Join(ConfigManager.Config.OsuPath, "collection.db");

            var db = DatabaseDecoder.DecodeOsu(osuDbPath);
            var collections = DatabaseDecoder.DecodeCollection(collectionPath);
            var beatmaps = db.Beatmaps;
            var collection = collections.Collections.FirstOrDefault(o => o.Name == collectionName);

            if (collection is null) {
                throw new CollectionNotFoundException(collectionName);
            }

            return collection.MD5Hashes.SelectMany(o => beatmaps.Where(b => b.MD5Hash == o));
        }

        public static OsuDatabase GetOsuDatabase() {
            var osuDbPath = Path.Join(ConfigManager.Config.OsuPath, "osu!.db");
            return DatabaseDecoder.DecodeOsu(osuDbPath);
        }

        public static List<DbBeatmap> GetAll() {
            return GetOsuDatabase().Beatmaps;
        }

        public static IEnumerable<DbBeatmap> GetMapSet(OsuDatabase db, int setId) {
            return db.Beatmaps.Where(o => o.BeatmapSetId == setId);
        }

        public static IEnumerable<DbBeatmap> GetFiltered(IHasFilter opts) {
            return (opts.CollectionName is null ? GetAll() : GetCollection(opts.CollectionName))
                .Where(o => DbBeatmapFilter(o, opts));
        }

        public static bool DbBeatmapFilter(DbBeatmap o, IHasFilter opts) {
            // Regex which matches any diffname with a possessive indicator to anyone other than the mapper
            var regex = new Regex(@$"(?!\s?(de\s)?(it|that|{string.Join('|', opts.Mapper!.Select(Regex.Escape))}))(((^|[^\S\r\n])(\S)*([sz]'|'s))|((^|[^\S\r\n])de\s(\S)*))", RegexOptions.IgnoreCase);

            return (!opts.MinId.HasValue || o.BeatmapSetId >= opts.MinId)
                   && (!opts.MaxId.HasValue || o.BeatmapSetId <= opts.MaxId)
                   && (!opts.RankedStatus!.Any() || opts.RankedStatus!.Contains(o.RankedStatus))
                   && o.Ruleset == opts.Ruleset
                   && (!opts.MinStarRating.HasValue || GetDefaultStarRating(o) >= opts.MinStarRating)
                   && (!opts.MaxStarRating.HasValue || GetDefaultStarRating(o) <= opts.MaxStarRating)
                   && (!opts.Mapper!.Any() || (opts.Mapper!.Any(x => x == o.Creator || o.Difficulty.Contains(x))
                                               && !o.Difficulty.Contains("Hitsounds", StringComparison.OrdinalIgnoreCase)
                                               && !o.Difficulty.Contains("Collab", StringComparison.OrdinalIgnoreCase)
                                               && !regex.IsMatch(o.Difficulty)));
        }

        public static double GetDefaultStarRating(DbBeatmap beatmap) {
            var dict = beatmap.Ruleset switch {
                Ruleset.Taiko => beatmap.TaikoStarRating,
                Ruleset.Mania => beatmap.ManiaStarRating,
                Ruleset.Fruits => beatmap.CatchStarRating,
                _ => beatmap.StandardStarRating
            };

            return dict.TryGetValue(Mods.None, out double value) ? value : double.NaN;
        }

        public static IEnumerable<IBeatmap> GetFilteredAndRead(IHasFilter opts) {
            return GetFiltered(opts)
                .Select(o => Path.Combine(ConfigManager.Config.SongsPath, o.FolderName.Trim(), o.FileName.Trim()))
                .Where((o) => {
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
                }).Where(ValidBeatmap)!;
        }

        public static IEnumerable<(IBeatmap, DbBeatmap)> GetFilteredAndRead2(IHasFilter opts) {
            return GetFiltered(opts)
                .Select(o => (Path.Combine(ConfigManager.Config.SongsPath, o.FolderName.Trim(), o.FileName.Trim()), o))
                .Where(o => {
                    if (File.Exists(o.Item1)) {
                        Console.Write('.');
                        return true;
                    }

                    Console.WriteLine(Strings.CouldNotFindFile, o.Item1);
                    return false;
                })
                .Select<(string, DbBeatmap), (IBeatmap?, DbBeatmap)>(o => {
                    try {
                        return (new BeatmapEditor(o.Item1).ReadFile(), o.Item2);
                    }
                    catch (Exception e) {
                        Console.WriteLine(Strings.ErrorReadingFile, o.Item1, e);
                        return (null, o.Item2);
                    }
                }).Where(o => ValidBeatmap(o.Item1))!;
        }

        public static bool ValidBeatmap(IBeatmap? beatmap) {
            if (beatmap == null)
                return false;
            // Non-finite timing points
            if (beatmap.BeatmapTiming.TimingPoints.Any(x => !double.IsFinite(x.MpB)))
                return false;
            // Extreme BPM
            if (beatmap.BeatmapTiming.Redlines.Any(x => x.GetBpm() > 10000 || x.GetBpm() < 1))
                return false;
            // Invisible circles
            if (beatmap.HitObjects.OfType<Slider>().Any(x => x.IsInvisible()))
                return false;
            // Extremely long sliders
            if (beatmap.HitObjects.OfType<Slider>().Any(x => x.PixelLength > 1000000))
                return false;
            return true;
        }
    }
}
