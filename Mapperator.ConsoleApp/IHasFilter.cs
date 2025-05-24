using System.Collections.Generic;
using OsuParsers.Enums;
using OsuParsers.Enums.Database;

namespace Mapperator.ConsoleApp;

public interface IHasFilter {
    public string? CollectionName { get; }
    int? MinId { get; }
    int? MaxId { get; }
    IEnumerable<RankedStatus>? RankedStatus { get; }
    Ruleset Ruleset { get; }
    double? MinStarRating { get; }
    double? MaxStarRating { get; }
    IEnumerable<string>? Mapper { get; }
}