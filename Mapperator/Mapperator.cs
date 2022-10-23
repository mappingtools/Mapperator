using Mapperator.Construction;
using Mapperator.Matching;
using Mapperator.Matching.DataStructures;
using Mapperator.Matching.Filters;
using Mapperator.Matching.Judges;
using Mapperator.Matching.Matchers;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;
using Mapping_Tools_Core.ToolHelpers;
using TrieNet;

namespace Mapperator;

public class Mapperator {
    private readonly TrieDataMatcher matcher;
    private readonly RhythmDistanceTrieStructure data;
    private readonly ReadOnlyMemory<MapDataPoint> pattern;
    private readonly SuperJudge judge;
    private readonly VisualSpacingJudge visualSpacingJudge;
    private readonly SliderAngleJudge sliderAngleJudge;
    private readonly BeatmapConstructor constructor;
    private readonly BestScoreFilter bestScoreFilter;
    private readonly BestScoreFilter bestScoreFilter2;
    private readonly OnScreenFilter onScreenFilter;
    private readonly TryMoreStuffFilter tryMoreStuffFilter;

    public Mapperator(RhythmDistanceTrieStructure data, ReadOnlyMemory<MapDataPoint> pattern, double lookBack, double objectRadius) {
        this.data = data;
        this.pattern = pattern;
        matcher = new TrieDataMatcher(data, pattern.Span);
        judge = new SuperJudge(pattern);
        bestScoreFilter = new BestScoreFilter(judge, 1000) { MinLengthProvider = matcher };
        visualSpacingJudge = new VisualSpacingJudge(pattern, lookBack, objectRadius);
        sliderAngleJudge = new SliderAngleJudge();
        bestScoreFilter2 = new BestScoreFilter(new JudgeMultiplier(judge, new IJudge[] { visualSpacingJudge, sliderAngleJudge }), 1);
        constructor = new BeatmapConstructor();
        onScreenFilter = new OnScreenFilter();
        tryMoreStuffFilter = new TryMoreStuffFilter();
    }

    /// <summary>
    /// Mapperates all of the pattern onto the list of hit objects.
    /// </summary>
    public Continuation MapPattern(IList<HitObject> hitObjects, Continuation? continuation = null, Timing? timing = null, List<ControlChange>? controlChanges = null) {
        var state = continuation ?? new Continuation(hitObjects);
        var pogs = 0;
        var failedMatches = 0;
        Match? lastMatch = null;

        for (var i = 0; i < pattern.Length; i++) {
            Match? pogMatch = lastMatch is { Length: > 1 } ? lastMatch.Value.Next() : null;

            onScreenFilter.Pos = state.Pos;
            onScreenFilter.Angle = state.Angle;
            judge.PatternIndex = i;
            visualSpacingJudge.Init(hitObjects, state, i);
            sliderAngleJudge.Angle = state.Angle;
            bestScoreFilter.PogMatch = pogMatch;
            judge.PogMatch = pogMatch;
            matcher.MinLength = 1;

            var matches = bestScoreFilter2.FilterMatches(
                tryMoreStuffFilter.FilterMatches(
                    bestScoreFilter.FilterMatches(
                        onScreenFilter.FilterMatches(
                            matcher.FindMatches(i)))));

            Match match;
            try {
                match = matches.First();
                Console.WriteLine($"match {i}, id = {match.SeqPos}, length = {match.Length}, min mult = {match.MinMult}, max mult = {match.MaxMult}");
            } catch (InvalidOperationException) {
                // No match was found, create a dummy match
                match = new Match(data.Data[0].AsMemory()[..1], new WordPosition<int>(0, 0), 1, 1);
                failedMatches++;
                Console.WriteLine($"match {i}, failed to find match!");
            }

            if (pogMatch.HasValue && match.SeqPos.Value == pogMatch.Value.SeqPos.Value
                                  && match.SeqPos.CharPosition == pogMatch.Value.SeqPos.CharPosition) pogs++;

            state = constructor.Construct(hitObjects, match, pattern.Span[i..], state, 1, timing, controlChanges);
            lastMatch = match;
        }

        Console.WriteLine($"Failed matches = {failedMatches}");
        Console.WriteLine($"Pograte = {(float)pogs / pattern.Length}");

        return state;
    }

    /// <summary>
    /// Mapperates all of the pattern onto the beatmap.
    /// </summary>
    public Continuation MapPattern(IBeatmap beatmap, Continuation? continuation = null) {
        var controlChanges = new List<ControlChange>();
        var state = MapPattern(beatmap.HitObjects, continuation, beatmap.BeatmapTiming, controlChanges);
        ControlChange.ApplyChanges(beatmap.BeatmapTiming, controlChanges);
        return state;
    }
}