using Mapperator.Resources;
using Mapping_Tools_Core.BeatmapHelper.IO.Editor;
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

            switch(args[0]) {
                case "-e":
                    if (!CheckLength(args, 3)) return;                   ;
                    string collectionName = args[1];
                    string outputName = args[2];
                    DoDataExtraction(collectionName, outputName);
                    break;
                case "-c":
                    throw new NotImplementedException();
                    break;
                default:
                    Console.WriteLine(string.Format(Strings.UnknownCommand, args[0]));
                    break;
            }
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
