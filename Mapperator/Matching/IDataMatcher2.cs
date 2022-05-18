namespace Mapperator.Matching;

public interface IDataMatcher2 {
    /// <summary>
    /// Finds all matching sequences at position i.
    /// </summary>
    /// <param name="i">The position in the pattern to find the matches for.</param>
    /// <returns>All matching sequences in roughly descending quality.</returns>
    IEnumerable<Match> FindMatches(int i);
}