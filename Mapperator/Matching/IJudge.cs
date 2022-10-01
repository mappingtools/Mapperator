using Mapperator.Model;

namespace Mapperator.Matching;

public interface IJudge {
    double Judge(ReadOnlySpan<MapDataPoint> foundPattern, ReadOnlySpan<MapDataPoint> wantedPattern, int lookBack, double mult);

    double MatchingCost(MapDataPoint expected, MapDataPoint actual, double mult);

    double RelationScore(ReadOnlySpan<MapDataPoint> expected, ReadOnlySpan<MapDataPoint> actual, int i, int j, double maxDiff, double mult);

    double BestPossibleScore(int length, int lookBack);

    int MinLengthForScore(double wantedScore);
}