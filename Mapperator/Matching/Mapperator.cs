using Mapperator.Construction;
using Mapperator.Matching.DataStructures;
using Mapperator.Matching.Filters;
using Mapperator.Matching.Matchers;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;
using Mapping_Tools_Core.ToolHelpers;
using TrieNet;

namespace Mapperator.Matching;

public class Mapperator {
    private readonly TrieDataMatcher2 matcher;
    private readonly RhythmDistanceTrieStructure data;
    private readonly ReadOnlyMemory<MapDataPoint> pattern;
    private readonly BeatmapConstructor2 constructor;
    private readonly BestScoreFilter bestScoreFilter;
    private readonly OnScreenFilter onScreenFilter;

    public Mapperator(RhythmDistanceTrieStructure data, ReadOnlyMemory<MapDataPoint> pattern, BeatmapConstructor2 constructor, IJudge judge, OnScreenFilter onScreenFilter) {
        matcher = new TrieDataMatcher2(data, pattern.Span);
        bestScoreFilter = new BestScoreFilter(judge, pattern, 1) { MinLengthProvider = matcher };
        this.data = data;
        this.pattern = pattern;
        this.constructor = constructor;
        this.onScreenFilter = onScreenFilter;
    }

    /// <summary>
    /// Mapperates all of the pattern onto the list of hit objects.
    /// </summary>
    public Continuation MapPattern(IList<HitObject> hitObjects, Continuation? continuation = null, Timing? timing = null, List<ControlChange>? controlChanges = null) {
        var state = continuation ?? new Continuation(hitObjects);
        Match? lastMatch = null;

        for (var i = 0; i < pattern.Length; i++) {
            onScreenFilter.Pos = state.Pos;
            onScreenFilter.Angle = state.Angle;
            bestScoreFilter.PatternIndex = i;
            bestScoreFilter.PogMatch = lastMatch is { Length: > 1 } ? lastMatch.Value.Next() : null;
            matcher.MinLength = 1;

            var matches = bestScoreFilter.FilterMatches(onScreenFilter.FilterMatches(matcher.FindMatches(i)));

            Match match;
            try {
                match = matches.First();
            } catch (InvalidOperationException) {
                // No match was found, create a dummy match
                match = new Match(data.Data[0].AsMemory()[..1], new WordPosition<int>(0, 0), 1, 1);
            }

            state = constructor.Construct(hitObjects, match, pattern.Span[i..], state, 1, timing, controlChanges);
            lastMatch = match;
        }

        return state;
    }
}