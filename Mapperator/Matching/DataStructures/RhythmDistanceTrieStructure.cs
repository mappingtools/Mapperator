using Gma.DataStructures.StringSearch;
using Mapperator.Model;
using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching.DataStructures;

public class RhythmDistanceTrieStructure {
    private readonly List<MapDataPoint[]> mapDataPoints = new();
    private readonly UkkonenTrie<ushort, int> rhythmTrie = new(1);

    public IReadOnlyList<MapDataPoint[]> Data => mapDataPoints;
    public UkkonenTrie<ushort, int> Trie => rhythmTrie;


    public ReadOnlyMemory<ushort> ToRhythmString(ReadOnlySpan<MapDataPoint> data) {
        var rhythmString = new ushort[data.Length];
        var i = 0;

        foreach (var mapDataPoint in data) {
            var ho = ToRhythmToken(mapDataPoint);
            rhythmString[i] = ho;
            i++;
        }

        return rhythmString.AsMemory();
    }

    public ushort ToRhythmToken(MapDataPoint mapDataPoint) {
        const int gapResolution = 6;
        const int gapRange = 9;
        var dist = (int) MathHelper.Clamp(mapDataPoint.Spacing / 4, 0, 255);
        var gap = MathHelper.Clamp((int) Math.Log2(mapDataPoint.BeatsSince) + gapResolution, 0, gapRange - 1);
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

    public (ReadOnlyMemory<ushort>, ReadOnlyMemory<ushort>) ToDistanceRange(ReadOnlySpan<ushort> query, Func<int, (int, int)> rangeFunc) {
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
}