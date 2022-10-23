using Mapperator.Model;

namespace Mapperator.Matching.Filters;

/// <summary>
/// Duplicates matches into slightly different lengths, rotations, and scalings
/// </summary>
public class TryMoreStuffFilter : IMatchFilter {
    public IEnumerable<Match> FilterMatches(IEnumerable<Match> matches) {
        foreach (var match in matches) {
            yield return match;

            var lengths = Enumerable.Range(1, match.Sequence.Length).ToArray();
            var angles = Enumerable.Range(-2, 5).Select(d => d * 0.2).ToArray();
            var multipliers = match.MaxMult - match.MinMult > 0.01
                ?  new[] { match.MinMult, Math.Sqrt(match.MinMult * match.MaxMult), match.MaxMult }
                : new[] { match.MinMult };

            var first = match.Sequence.Span[0];
            foreach (var length in lengths) {
                foreach (var angle in angles) {
                    foreach (var multiplier in multipliers) {
                        var newSequence = match.Sequence[..length].ToArray();
                        newSequence[0] = new MapDataPoint(first.DataType, first.BeatsSince, first.Spacing,
                            first.Angle + angle, first.NewCombo, first.SliderType, first.SliderLength,
                            first.SliderSegments, first.Repeats, first.HitObject);

                        yield return new Match(newSequence.AsMemory(), match.SeqPos, multiplier, multiplier);
                    }
                }
            }
        }
    }
}