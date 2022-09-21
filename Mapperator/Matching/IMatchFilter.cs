namespace Mapperator.Matching;

public interface IMatchFilter {
    IEnumerable<Match> FilterMatches(IEnumerable<Match> matches);
}