using CommandLine;
using Mapperator.ConsoleApp.Verbs;
using Convert = Mapperator.ConsoleApp.Verbs.Convert;

namespace Mapperator.ConsoleApp {
    public static class Program {
        private static int Main(string[] args) {
            ConfigManager.LoadConfig();

            return Parser.Default.ParseArguments<Count.CountOptions, Extract.ExtractOptions, Build.BuildOptions, Convert.ConvertOptions, Search.SearchOptions, Analyze.AnalyzeOptions>(args)
              .MapResult(
                  (Count.CountOptions opts) => Count.DoDataCount(opts),
                (Extract.ExtractOptions opts) => Extract.DoDataExtraction(opts),
                (Build.BuildOptions opts) => Build.DoBuildGraph(opts),
                (Convert.ConvertOptions opts) => Convert.DoMapConvert(opts),
                (Search.SearchOptions opts) => Search.DoPatternSearch(opts),
                (Analyze.AnalyzeOptions opts) => Analyze.DoVisualSpacingExtract(opts),
                _ => 1);
        }
    }
}
