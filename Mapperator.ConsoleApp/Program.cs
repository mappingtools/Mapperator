using CommandLine;
using Mapperator.ConsoleApp.Verbs;
using Convert = Mapperator.ConsoleApp.Verbs.Convert;

namespace Mapperator.ConsoleApp {
    public static class Program {
        private static int Main(string[] args) {
            ConfigManager.LoadConfig();

            return Parser.Default
                .ParseArguments<
                    Count.CountOptions,
                    Extract.ExtractOptions,
                    Build.BuildOptions,
                    Convert.ConvertOptions,
                    Search.SearchOptions,
                    Analyze.AnalyzeOptions,
                    Extract2.Extract2Options,
                    Dataset.DatasetOptions,
                    Dataset2.DatasetOptions2
                >(args)
                .MapResult(
                    (Count.CountOptions opts) => Count.DoDataCount(opts),
                    (Extract.ExtractOptions opts) => Extract.DoDataExtraction(opts),
                    (Build.BuildOptions opts) => Build.DoBuildGraph(opts),
                    (Convert.ConvertOptions opts) => Convert.DoMapConvert(opts),
                    (Search.SearchOptions opts) => Search.DoPatternSearch(opts),
                    (Analyze.AnalyzeOptions opts) => Analyze.DoVisualSpacingExtract(opts),
                    (Extract2.Extract2Options opts) => Extract2.DoDataExtraction(opts),
                    (Dataset.DatasetOptions opts) => Dataset.DoDataExtraction(opts),
                    (Dataset2.DatasetOptions2 opts) => Dataset2.DoDataExtraction2(opts),
                    _ => 1);
        }
    }
}
