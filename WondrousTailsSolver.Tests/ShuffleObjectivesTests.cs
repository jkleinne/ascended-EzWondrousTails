using WondrousTailsSolver;
using Xunit;

public class ShuffleObjectivesTests {
    [Theory]
    [InlineData((ShuffleObjective)999, ShuffleObjective.TwoLineMax)]
    [InlineData(ShuffleObjective.OneLineMax, ShuffleObjective.OneLineMax)]
    [InlineData(ShuffleObjective.ThreeLineMax, ShuffleObjective.ThreeLineMax)]
    [InlineData(ShuffleObjective.RewardBalanced, ShuffleObjective.RewardBalanced)]
    public void Normalize_ClampsUndefinedToDefault(ShuffleObjective input, ShuffleObjective expected) {
        Assert.Equal(expected, ShuffleObjectives.Normalize(input));
    }
}
