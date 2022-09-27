using Gma.DataStructures.StringSearch;
using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching.DataStructures;

public class RhythmDistanceTrieStructure {
    private readonly List<MapDataPoint[]> mapDataPoints = new();

    public IReadOnlyList<MapDataPoint[]> Data => mapDataPoints;
    public RhythmDistanceTrie Trie { get; } = new();

    public void Add(MapDataPoint[] data) {
        Trie.Add(ToRhythmString(data.AsSpan()), mapDataPoints.Count);
        mapDataPoints.Add(data);
    }

    public static ReadOnlyMemory<RhythmToken> ToRhythmString(ReadOnlySpan<MapDataPoint> data) {
        var rhythmString = new RhythmToken[data.Length];
        var i = 0;

        foreach (var mapDataPoint in data) {
            var ho = ToRhythmToken(mapDataPoint);
            rhythmString[i] = ho;
            i++;
        }

        return rhythmString.AsMemory();
    }

    public static RhythmToken ToRhythmToken(MapDataPoint mapDataPoint) {
        const int gapResolution = 6;
        const int gapRange = 9;

        var dist = MathHelper.Clamp((int)mapDataPoint.Spacing / 4, 0, 255);
        var gap = MathHelper.Clamp((int) Math.Round(Math.Log2(mapDataPoint.BeatsSince) + gapResolution), 0, gapRange - 1);

        if (mapDataPoint.DataType == DataType.Release) {
            // The beat gap is less important for selecting sliders so lets reduce gap to just 2 classes for sliders
            // If its less than 1/2 beat, its 0
            gap = gap < 5 ? 0 : gap > 5 ? 2 : 1;
            // Lets also allow a coarser range of distances
            dist = MathHelper.Clamp((int)mapDataPoint.Spacing / 12, 0, 255);
        }

        // Type of hit: hit, spin, release, release + 1 repeat, release + >1 repeats.
        var type = mapDataPoint.DataType switch {
            DataType.Hit => 0,
            DataType.Spin => 1,
            DataType.Release => mapDataPoint.Repeats switch {
                0 => 2,
                1 => 3,
                _ => 4
            },
            _ => 0
        };

        // (11112222) dataType2 <= 4 so 3 bits is enough, and gap <= 8 so 4 bits is enough
        return new RhythmToken(type, gap, dist);
    }

    public static (ReadOnlyMemory<RhythmToken>, ReadOnlyMemory<RhythmToken>) ToDistanceRange(ReadOnlySpan<RhythmToken> query, Func<int, (int, int)> rangeFunc) {
        var min = new RhythmToken[query.Length];
        var max = new RhythmToken[query.Length];

        for (var i = 0; i < query.Length; i++) {
            var token = query[i];
            var type = token.Type;
            var gap = token.Gap;
            var (mini, maxi) = rangeFunc(token.Dist);
            var minDistance = MathHelper.Clamp(mini, 0, 255);
            var maxDistance = MathHelper.Clamp(maxi, 0, 255);
            min[i] = new RhythmToken(type, gap, minDistance);
            max[i] = new RhythmToken(type, gap, maxDistance);
        }

        return (min.AsMemory(), max.AsMemory());
    }

    public static (ReadOnlyMemory<RhythmToken>, ReadOnlyMemory<RhythmToken>) ToDistanceRange(ReadOnlySpan<RhythmToken> query, int width) {
        return ToDistanceRange(query, d => ((int)(d * 0.66 - width), (int)(d * 1.5 + width)));
    }

    public static (ReadOnlyMemory<RhythmToken>, ReadOnlyMemory<RhythmToken>)[] ToDistanceRanges(ReadOnlySpan<RhythmToken> query, int[] widths) {
        var ranges = new (ReadOnlyMemory<RhythmToken>, ReadOnlyMemory<RhythmToken>)[widths.Length];

        for (var i = 0; i < widths.Length; i++) {
            ranges[i] = ToDistanceRange(query, widths[i]);
        }

        return ranges;
    }

    public MapDataPoint GetMapDataPoint(WordPosition<int> wordPosition, int offset = 0) {
        return mapDataPoints[wordPosition.Value][wordPosition.CharPosition + offset];
    }

    public bool WordPositionInRange(WordPosition<int> wordPosition, int offset = 0) {
        return wordPosition.Value < mapDataPoints.Count && wordPosition.Value >= 0 &&
               wordPosition.CharPosition + offset < mapDataPoints[wordPosition.Value].Length &&
               wordPosition.CharPosition + offset >= 0;
    }
}