using Mapperator.Model;

namespace Mapperator.Matching.Filters;

/// <summary>
/// Makes sure the filtered matches stay completely on screen when placed in the editor.
/// </summary>
public class BestScoreOrderFilter : IMatchFilter {
    private const int BufferSize = 1000;

    private readonly IJudge judge;
    private readonly ReadOnlyMemory<MapDataPoint> pattern;
    private readonly IMinLengthProvider? minLengthProvider;

    private readonly PriorityQueue<Match, double> queue = new(BufferSize);

    public int PatternIndex { get; set; }

    public BestScoreOrderFilter(IJudge judge, ReadOnlyMemory<MapDataPoint> pattern, IMinLengthProvider? minLengthProvider = null) {
        this.judge = judge;
        this.pattern = pattern;
        this.minLengthProvider = minLengthProvider;
    }

    public IEnumerable<Match> FilterMatches(IEnumerable<Match> matches) {
        queue.Clear();
        var bestScore = double.NegativeInfinity;

        foreach (var match in matches) {
            var score = RateMatchQuality(match);

            if (queue.Count >= BufferSize) {
                yield return queue.EnqueueDequeue(match, -score);
            } else {
                queue.Enqueue(match, -score);
            }

            if (score > bestScore && minLengthProvider is not null) {
                bestScore = score;
                minLengthProvider.MinLength = Math.Max(minLengthProvider.MinLength, judge.MinLengthForScore(bestScore));
            }
        }

        while (queue.Count > 0) {
            yield return queue.Dequeue();
        }
    }

    private double RateMatchQuality(Match match) {
        var mult = match.MinMult == 0 && double.IsPositiveInfinity(match.MaxMult) ? 1 : Math.Sqrt(match.MinMult * match.MaxMult);
        return judge.Judge(match.Sequence.Span, pattern.Span.Slice(PatternIndex, match.Length), 0, mult);
    }
}