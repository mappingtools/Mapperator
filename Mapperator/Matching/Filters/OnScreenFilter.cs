using Mapping_Tools_Core.MathUtil;

namespace Mapperator.Matching.Filters;

/// <summary>
/// Makes sure the filtered matches stay completely on screen when placed in the editor.
/// </summary>
public class OnScreenFilter : IMatchFilter {
    /// <summary>
    /// The position of the last map data point before the wanted index.
    /// </summary>
    public Vector2 Pos { get; set; }

    /// <summary>
    /// The angle of the last map data point before the wanted index.
    /// </summary>
    public double Angle { get; set; }

    public IEnumerable<Match> FilterMatches(IEnumerable<Match> matches) {
        foreach (var match in matches) {
            var length = ValidLength(match);

            if (length == 0) continue;

            if (length == match.WholeSequence.Length - match.Lookback) yield return match;

            // Cut the match length until the part where it stops being valid
            yield return new Match(match.WholeSequence[..(match.Lookback + length)], match.Lookback, match.SeqPos);
        }
    }

    private int ValidLength(Match match) {
        double angle = Angle;
        var pos = Pos;
        var length = 0;
        for (var i = match.Lookback; i < match.WholeSequence.Length; i++) {
            var dataPoint = match.WholeSequence.Span[i];

            angle += dataPoint.Angle;
            var dir = Vector2.Rotate(Vector2.UnitX, angle);
            pos += dataPoint.Spacing * dir;

            if (!PosInBounds(pos)) return length;

            length++;
        }

        return length;
    }

    private static bool PosInBounds(Vector2 pos) {
        return pos.X is >= -5 and <= 517 && pos.Y is >= -5 and <= 387;
    }
}