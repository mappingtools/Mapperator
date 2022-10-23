namespace Mapperator.Matching.Judges; 

public class JudgeMultiplier : IJudge {
    private readonly IJudge judge;
    private readonly IJudge[] multipliers;

    public JudgeMultiplier(IJudge judge, IJudge[] multipliers) {
        this.judge = judge;
        this.multipliers = multipliers;
    }

    public double Judge(Match match) {
        var score = judge.Judge(match);
        foreach (var multiplier in multipliers) {
            score *= multiplier.Judge(match);
        }

        return score;
    }

    public int MinLengthForScore(double wantedScore) {
        return 1;
    }
}