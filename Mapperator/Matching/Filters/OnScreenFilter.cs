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
    private double Angle { get; set; }

    public IEnumerable<Match> FilterMatches(IEnumerable<Match> matches) {
        return matches.Where(IsValidSeries);
    }

    private bool IsValidSeries(Match match) {
        double angle = Angle;
        var pos = Pos;
        for (var i = match.Lookback; i < match.WholeSequence.Length; i++) {
            var dataPoint = match.WholeSequence.Span[i];

            angle += dataPoint.Angle;
            var dir = Vector2.Rotate(Vector2.UnitX, angle);
            pos += dataPoint.Spacing * dir;

            if (!PosInBounds(pos)) return false;
        }

        return true;
    }

    private static bool PosInBounds(Vector2 pos) {
        return pos.X is >= -5 and <= 517 && pos.Y is >= -5 and <= 387;
    }
}