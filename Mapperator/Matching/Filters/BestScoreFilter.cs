
namespace Mapperator.Matching.Filters;

/// <summary>
/// Exhausts the enumerable and yields the <see cref="BufferSize"/> number of best matches.
/// </summary>
public class BestScoreFilter : IMatchFilter {
    private readonly IJudge judge;

    private readonly PriorityQueue<(Match, double), double> queue;
    private readonly Match[] bestMatches;
    private readonly object queueLock = new();

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
            var score = judge.Judge(PogMatch.Value);
            queue.Enqueue((PogMatch.Value, score), score);

            if (score > bestScore && MinLengthProvider is not null) {
                bestScore = score;
                MinLengthProvider.MinLength = Math.Max(MinLengthProvider.MinLength, judge.MinLengthForScore(bestScore));
            }
        }

        const int nTasks = 1000;
        var t = 0;
        var tasks = new Task[nTasks];
        for (var i = 0; i < nTasks; i++) {
            tasks[i] = Task.CompletedTask;
        }
        foreach (var match in matches) {
            tasks[t++] = Task.Run(() => {
                var score = judge.Judge(match);

                lock(queueLock) {
                    if (queue.Count < BufferSize) {
                        queue.Enqueue((match, score), score);
                    }
                    else {
                        var (_, minScore) = queue.Peek();
                        if (score <= minScore) return;

                        queue.EnqueueDequeue((match, score), score);
                    }

                    if (!(score > bestScore) || MinLengthProvider is null) return;

                    bestScore = score;
                    MinLengthProvider.MinLength = Math.Max(MinLengthProvider.MinLength, judge.MinLengthForScore(bestScore));
                }
            });

            if (t != nTasks) continue;
            Task.WaitAll(tasks);
            t = 0;
        }

        Task.WaitAll(tasks);

        var n = Math.Min(BufferSize, queue.Count);
        for (var i = 0; i < n; i++) {
            bestMatches[n - i - 1] = queue.Dequeue().Item1;
        }

        for (var i = 0; i < n; i++) {
            yield return bestMatches[i];
        }
    }
}