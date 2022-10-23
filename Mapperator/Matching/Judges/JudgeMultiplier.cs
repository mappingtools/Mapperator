namespace Mapperator.Matching.Judges; 

public class JudgeMultiplier : IJudge {
    private readonly IJudge judge;
    private readonly IJudge multiplier;

    public JudgeMultiplier(IJudge judge, IJudge multiplier) {
        this.judge = judge;
        this.multiplier = multiplier;
    }

    public double Judge(Match match) {
        return judge.Judge(match) * multiplier.Judge(match);
    }

    public int MinLengthForScore(double wantedScore) {
        return 1;
    }

    public double PogScore() {
        return judge.PogScore();
    }
}