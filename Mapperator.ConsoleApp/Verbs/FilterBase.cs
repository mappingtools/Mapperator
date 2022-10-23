using System.Collections.Generic;
using CommandLine;
using JetBrains.Annotations;
using OsuParsers.Enums;
using OsuParsers.Enums.Database;

namespace Mapperator.ConsoleApp.Verbs;

public abstract class FilterBase : IHasFilter {
    [Option('c', "collection", HelpText = "Name of osu! collection to be extracted.")]
    public string? CollectionName { get; [UsedImplicitly] set; }

    [Option('i', "minId", HelpText = "Filter the minimum beatmap set ID.")]
    public int? MinId { get; [UsedImplicitly] set; }

    [Option('s', "status", HelpText = "Filter the ranked status.", Separator = ',')]
    public IEnumerable<RankedStatus>? RankedStatus { get; [UsedImplicitly] set; }

    [Option('m', "mode", HelpText = "Filter the game mode.", Default = Ruleset.Standard)]
    public Ruleset Ruleset { get; [UsedImplicitly] set; }

    [Option('r', "starRating", HelpText = "Filter the star rating.")]
    public double? MinStarRating { get; [UsedImplicitly] set; }

    [Option('a', "mapper", HelpText = "Filter on mapper name.", Separator = ',')]
    public IEnumerable<string>? Mapper { get; [UsedImplicitly] set; }
}