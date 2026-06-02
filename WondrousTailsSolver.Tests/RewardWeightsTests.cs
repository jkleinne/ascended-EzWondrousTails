using WondrousTailsSolver;
using Xunit;

public class RewardWeightsTests {
    [Fact]
    public void ScoreFor_ExistingObjectives_ReproduceCumulativeFormulas() {
        var chances = new LineChances(0.5, 0.3, 0.1);

        Assert.Equal(0.5, chances.ScoreFor(ShuffleObjective.OneLineMax), 10);            // P(>=1)
        Assert.Equal(0.3, chances.ScoreFor(ShuffleObjective.TwoLineMax), 10);            // P(>=2)
        Assert.Equal(0.1, chances.ScoreFor(ShuffleObjective.ThreeLineMax), 10);          // P(=3)
        Assert.Equal(0.8, chances.ScoreFor(ShuffleObjective.OneAndTwoLineTradeoff), 10); // P(>=1)+P(>=2)
    }

    [Fact]
    public void For_MapsEachObjectiveToItsWeightVector() {
        Assert.Equal(new RewardWeights(1, 1, 1), RewardWeights.For(ShuffleObjective.OneLineMax));
        Assert.Equal(new RewardWeights(0, 1, 1), RewardWeights.For(ShuffleObjective.TwoLineMax));
        Assert.Equal(new RewardWeights(0, 0, 1), RewardWeights.For(ShuffleObjective.ThreeLineMax));
        Assert.Equal(new RewardWeights(1, 2, 2), RewardWeights.For(ShuffleObjective.OneAndTwoLineTradeoff));
        Assert.Equal(new RewardWeights(1, 3, 9), RewardWeights.For(ShuffleObjective.RewardBalanced));
    }

    [Fact]
    public void ExpectedReward_WeightsExactTierProbabilities() {
        // Cumulative (0.5, 0.3, 0.1) -> exact tiers P(=1)=0.2, P(=2)=0.2, P(=3)=0.1.
        var chances = new LineChances(0.5, 0.3, 0.1);
        var weights = new RewardWeights(1, 3, 9);

        // 1*0.2 + 3*0.2 + 9*0.1 = 1.7
        Assert.Equal(1.7, weights.ExpectedReward(chances), 10);
    }

    [Fact]
    public void RewardBalanced_RewardsThreeLinesAboveTwoLineMax() {
        var chances = new LineChances(0.9, 0.5, 0.2);
        var balanced = RewardWeights.For(ShuffleObjective.RewardBalanced);
        var twoLineMax = RewardWeights.For(ShuffleObjective.TwoLineMax);

        Assert.True(balanced.ExpectedReward(chances) > twoLineMax.ExpectedReward(chances));
    }
}
