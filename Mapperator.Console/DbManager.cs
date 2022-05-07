using OsuParsers.Database.Objects;
using OsuParsers.Decoders;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapperator.Console.Exceptions;

namespace Mapperator.Console {
    public static class DbManager {
        public static IEnumerable<DbBeatmap> GetCollection(string collectionName) {
            string osuDbPath = Path.Join(ConfigManager.Config.OsuPath, "osu!.db");
            string collectionPath = Path.Join(ConfigManager.Config.OsuPath, "collection.db");

            var db = DatabaseDecoder.DecodeOsu(osuDbPath);
            var collections = DatabaseDecoder.DecodeCollection(collectionPath);
            var beatmaps = db.Beatmaps;
            var collection = collections.Collections.FirstOrDefault(o => o.Name == collectionName);

            if (collection is null) {
                throw new CollectionNotFoundException(collectionName);
            }

            return collection.MD5Hashes.SelectMany(o => beatmaps.Where(b => b.MD5Hash == o));
        }
    }
}
