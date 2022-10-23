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
        Console.WriteLine(string.Join(';',
            Analyzer.ExtractVisualSpacing(DbManager.GetFilteredAndRead(opts))
                .Select(o => o.ToString(CultureInfo.CurrentCulture))));

        return 0;
    }
}