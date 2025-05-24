namespace Mapperator.Matching;

public interface IJudge {
    double Judge(Match match);

    int MinLengthForScore(double wantedScore);
}