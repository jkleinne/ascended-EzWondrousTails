using System.Collections.Generic;
using WondrousTailsSolver;
using Xunit;

public class ShufflePolicyTests {
    [Fact]
    public void ComputeShuffleThresholds_FirstIsMean_AndNonDecreasing() {
        double[] scores = [0.0, 0.2, 0.4, 1.0];

        var thetas = ShufflePolicy.ComputeShuffleThresholds(scores, 4);

        Assert.Equal(0.4, thetas[0], 10); // mean
        for (var index = 1; index < thetas.Length; index++) {
            Assert.True(thetas[index] >= thetas[index - 1]);
        }
        Assert.True(thetas[^1] <= 1.0); // bounded by max
    }

    [Fact]
    public void ComputeShuffleThresholds_SecondMatchesHandComputation() {
        double[] scores = [0.0, 1.0]; // mean 0.5

        var thetas = ShufflePolicy.ComputeShuffleThresholds(scores, 2);

        // theta2 = E[max(X, 0.5)] = (0.5 + 1.0) / 2 = 0.75
        Assert.Equal(0.5, thetas[0], 10);
        Assert.Equal(0.75, thetas[1], 10);
    }

    [Theory]
    [InlineData(0.30, ShuffleRecommendation.StrongKeep)] // gap >= 0.25*sigma
    [InlineData(0.10, ShuffleRecommendation.Keep)]       // 0 <= gap < 0.25*sigma
    [InlineData(0.0, ShuffleRecommendation.Keep)]
    [InlineData(-0.05, ShuffleRecommendation.Neutral)]   // -0.10*sigma < gap < 0
    [InlineData(-0.20, ShuffleRecommendation.Shuffle)]   // gap <= -0.10*sigma
    public void VerdictForGap_ScalesBandsToSigma(double gap, ShuffleRecommendation expected) {
        Assert.Equal(expected, ShufflePolicy.VerdictForGap(gap, sigma: 1.0));
    }

    [Fact]
    public void VerdictForGap_BandsShrinkWithSmallerSigma() {
        // sigma 0.1 -> StrongKeep edge 0.025, shuffle edge -0.01.
        Assert.Equal(ShuffleRecommendation.StrongKeep, ShufflePolicy.VerdictForGap(0.03, sigma: 0.1));
        Assert.Equal(ShuffleRecommendation.Shuffle, ShufflePolicy.VerdictForGap(-0.02, sigma: 0.1));
    }

    [Fact]
    public void Evaluate_OutOfRange_IsUnavailable() {
        var policy = new ShufflePolicy(new Dictionary<int, IReadOnlyList<LineChances>> {
            [5] = [new LineChances(0.5, 0.3, 0.1)],
        });
        var request = new ShuffleAdviceRequest(new LineChances(0.5, 0.3, 0.1), 9, 9, ShuffleObjective.TwoLineMax);

        Assert.Equal(ShuffleRecommendation.Unavailable, policy.Evaluate(request).Recommendation);
    }

    [Fact]
    public void Evaluate_InsufficientPoints_IsNeedSecondChance() {
        var policy = new ShufflePolicy(new Dictionary<int, IReadOnlyList<LineChances>> {
            [5] = [new LineChances(0.5, 0.3, 0.1), new LineChances(0.9, 0.7, 0.3)],
        });
        var request = new ShuffleAdviceRequest(new LineChances(0.5, 0.3, 0.1), 5, 1, ShuffleObjective.TwoLineMax);

        var verdict = policy.Evaluate(request);

        Assert.Equal(ShuffleRecommendation.NeedSecondChance, verdict.Recommendation);
        Assert.Equal(0, verdict.AffordableShuffles);
    }

    [Fact]
    public void Evaluate_BudgetTipsKeepToShuffle() {
        // Heavy upper tail: nine low boards (TwoLines 0.20), one rare high board (0.90).
        // mean theta1 = (9*0.20 + 0.90)/10 = 0.27; sigma = 0.21; theta4 = 0.44073.
        var boards = new List<LineChances>();
        for (var index = 0; index < 9; index++) {
            boards.Add(new LineChances(0.30, 0.20, 0.0));
        }
        boards.Add(new LineChances(0.95, 0.90, 0.0));
        var policy = new ShufflePolicy(new Dictionary<int, IReadOnlyList<LineChances>> { [5] = boards });

        var current = new LineChances(0.50, 0.40, 0.0); // TwoLineMax score 0.40
        var atOneShuffle = policy.Evaluate(new ShuffleAdviceRequest(current, 5, 2, ShuffleObjective.TwoLineMax));
        var atFourShuffles = policy.Evaluate(new ShuffleAdviceRequest(current, 5, 8, ShuffleObjective.TwoLineMax));

        // gap at 1 shuffle: 0.40-0.27=+0.13 >= 0.25*0.21 -> StrongKeep.
        // gap at 4 shuffles: 0.40-0.44073=-0.04073 <= -0.10*0.21 -> Shuffle.
        Assert.Equal(ShuffleRecommendation.StrongKeep, atOneShuffle.Recommendation);
        Assert.Equal(ShuffleRecommendation.Shuffle, atFourShuffles.Recommendation);
        Assert.Equal(4, atFourShuffles.AffordableShuffles);
    }
}
