
namespace Mapperator.Matching.Filters;

/// <summary>
/// Exhausts the enumerable and yields the <see cref="BufferSize"/> number of best matches.
/// </summary>
public class BestScoreFilter : IMatchFilter {
    private readonly IJudge judge;

    private readonly PriorityQueue<(Match, double), double> queue;
    private readonly Match[] bestMatches;

    public int BufferSize { get; }

    public IMinLengthProvider? MinLengthProvider { get; init; }

    public Match? PogMatch { get; set; }

    public BestScoreFilter(IJudge judge, int bufferSize) {
        if (bufferSize < 1) throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, @"Buffer size must be at least 1.");

        this.judge = judge;
        BufferSize = bufferSize;
        queue = new PriorityQueue<(Match, double), double>(BufferSize);
        bestMatches = new Match[BufferSize];
    }

    public IEnumerable<Match> FilterMatches(IEnumerable<Match> matches) {
        queue.Clear();
        var bestScore = double.NegativeInfinity;

        if (PogMatch.HasValue) {
            var score = judge.Judge(PogMatch.Value) + judge.PogScore();
            queue.Enqueue((PogMatch.Value, score), score);

            if (score > bestScore && MinLengthProvider is not null) {
                bestScore = score;
                MinLengthProvider.MinLength = Math.Max(MinLengthProvider.MinLength, judge.MinLengthForScore(bestScore));
            }
        }

        foreach (var match in matches) {
            var score = judge.Judge(match);

            if (queue.Count < BufferSize) {
                queue.Enqueue((match, score), score);
            } else {
                var (_, minScore) = queue.Peek();
                if (score <= minScore) continue;

                queue.EnqueueDequeue((match, score), score);
            }

            if (!(score > bestScore) || MinLengthProvider is null) continue;

            bestScore = score;
            MinLengthProvider.MinLength = Math.Max(MinLengthProvider.MinLength, judge.MinLengthForScore(bestScore));
        }

        var n = Math.Min(BufferSize, queue.Count);
        for (var i = 0; i < n; i++) {
            bestMatches[n - i - 1] = queue.Dequeue().Item1;
        }

        for (var i = 0; i < n; i++) {
            yield return bestMatches[i];
        }
    }
}