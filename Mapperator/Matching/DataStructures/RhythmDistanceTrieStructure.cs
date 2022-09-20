using Gma.DataStructures.StringSearch;
using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching.DataStructures;

public class RhythmDistanceTrieStructure {
    private readonly List<MapDataPoint[]> mapDataPoints = new();

    public IReadOnlyList<MapDataPoint[]> Data => mapDataPoints;
    public UkkonenTrie<ushort, int> Trie { get; } = new(1);

    public void Add(MapDataPoint[] data) {
        Trie.Add(ToRhythmString(data.AsSpan()), mapDataPoints.Count);
        mapDataPoints.Add(data);
    }

    public static ReadOnlyMemory<ushort> ToRhythmString(ReadOnlySpan<MapDataPoint> data) {
        var rhythmString = new ushort[data.Length];
        var i = 0;

        foreach (var mapDataPoint in data) {
            var ho = ToRhythmToken(mapDataPoint);
            rhythmString[i] = ho;
            i++;
        }

        return rhythmString.AsMemory();
    }

    public static ushort ToRhythmToken(MapDataPoint mapDataPoint) {
        const int gapResolution = 6;
        const int gapRange = 9;
        var dist = (int) MathHelper.Clamp(mapDataPoint.Spacing / 4, 0, 255);
        var gap = MathHelper.Clamp((int) Math.Round(Math.Log2(mapDataPoint.BeatsSince) + gapResolution), 0, gapRange - 1);

        if (mapDataPoint.DataType == DataType.Release) {
            // The beat gap is less important for selecting sliders so lets reduce gap to just 2 classes for sliders
            // If its less than 1/2 beat, its 0
            gap = gap < 5 ? 0 : 1;
            // Lets also allow a coarser range of distances
            dist = (int) MathHelper.Clamp(mapDataPoint.Spacing / 12, 0, 255);
        }

        var typeByte = mapDataPoint.DataType switch {
            DataType.Hit => gap,
            DataType.Spin => gapRange + gap,
            DataType.Release => mapDataPoint.Repeats switch {
                0 => gapRange * 2 + gap,
                1 => gapRange * 3 + gap,
                _ => gapRange * 4 + gap
            },
            _ => gap
        };
        // (type   ,dist    )
        // (1111111100000000)
        return (ushort)((typeByte << 8) | dist);
    }

    public static (ReadOnlyMemory<ushort>, ReadOnlyMemory<ushort>) ToDistanceRange(ReadOnlySpan<ushort> query, Func<int, (int, int)> rangeFunc) {
        var min = new ushort[query.Length];
        var max = new ushort[query.Length];
        for (var i = 0; i < query.Length; i++) {
            var token = query[i];
            var rhythmPart = token >> 8;
            var distancePart = token & 255;
            var (mini, maxi) = rangeFunc(distancePart);
            var minDistance = MathHelper.Clamp(mini, 0, 255);
            var maxDistance = MathHelper.Clamp(maxi, 0, 255);
            min[i] = (ushort)((rhythmPart << 8) | minDistance);
            max[i] = (ushort)((rhythmPart << 8) | maxDistance);
        }

        return (min.AsMemory(), max.AsMemory());
    }

    public static (ReadOnlyMemory<ushort>, ReadOnlyMemory<ushort>) ToDistanceRange(ReadOnlySpan<ushort> query, int width) {
        return ToDistanceRange(query, d => (d - width, d + width));
    }

    public static (ReadOnlyMemory<ushort>, ReadOnlyMemory<ushort>)[] ToDistanceRanges(ReadOnlySpan<ushort> query, double[] widths) {
        var ranges = new (ReadOnlyMemory<ushort>, ReadOnlyMemory<ushort>)[widths.Length];

        for (var i = 0; i < widths.Length; i++) {
            var width = (int)widths[i];
            ranges[i] = ToDistanceRange(query, width);
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