using System;
using System.Globalization;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;

namespace Mapperator.ConsoleApp.Verbs;

public static class Analyze {
    [Verb("analyze", HelpText = "Extract visual spacing and slider angle statistics.")]
    [UsedImplicitly]
    public class AnalyzeOptions : FilterBase { }

    public static int DoVisualSpacingExtract(AnalyzeOptions opts) {
        var extractor = new DistanceAnalyser();
        Console.WriteLine(string.Join(';',
            extractor.ExtractVisualSpacing(DbManager.GetFilteredAndRead(opts))
                .Select(o => o.ToString(CultureInfo.CurrentCulture))));

        return 0;
    }
}