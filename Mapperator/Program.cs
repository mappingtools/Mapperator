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
                    if (!CheckLength(args, 4)) return; ;
                    dataName = args[1];
                    inputName = args[2];
                    outputName = args[3];
                    DoMapConvert(dataName, inputName, outputName);
                    break;
                default:
                    Console.WriteLine(string.Format(Strings.UnknownCommand, args[0]));
                    break;
            }
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
