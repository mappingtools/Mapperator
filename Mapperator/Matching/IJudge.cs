using Mapperator.Model;

namespace Mapperator.Matching;

public interface IJudge {
    double Judge(ReadOnlySpan<MapDataPoint> foundPattern, ReadOnlySpan<MapDataPoint> wantedPattern, int lookBack);

    double BestPossibleScore(int length, int lookBack);
}