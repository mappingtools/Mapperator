using System.Collections.Generic;
using OsuParsers.Enums;
using OsuParsers.Enums.Database;

namespace Mapperator.ConsoleApp;

public interface IHasFilter {
    public string? CollectionName { get; }
    int? MinId { get; }
    IEnumerable<RankedStatus>? RankedStatus { get; }
    Ruleset Ruleset { get; }
    double? MinStarRating { get; }
    IEnumerable<string>? Mapper { get; }
}