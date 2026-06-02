using WondrousTailsSolver;
using Xunit;

public class ShuffleAdviceTests {
    private static bool[] Board(params int[] filledIndices) {
        var cells = new bool[WondrousTailsBoardSolver.CellCount];
        foreach (var index in filledIndices) {
            cells[index] = true;
        }
        return cells;
    }

    [Theory]
    [InlineData(0.01, ShuffleRecommendation.StrongKeep)]
    [InlineData(0.02, ShuffleRecommendation.StrongKeep)]
    [InlineData(0.005, ShuffleRecommendation.Keep)]
    [InlineData(0.0, ShuffleRecommendation.Keep)]
    [InlineData(-0.003, ShuffleRecommendation.Neutral)]
    [InlineData(-0.005, ShuffleRecommendation.Shuffle)]
    [InlineData(-0.02, ShuffleRecommendation.Shuffle)]
    public void RecommendationForDelta_MapsBoundaries(double delta, ShuffleRecommendation expected) {
        Assert.Equal(expected, WondrousTailsBoardSolver.RecommendationForDelta(delta));
    }

    [Fact]
    public void GetShuffleAdvice_OutOfRange_IsUnavailable() {
        var solver = new WondrousTailsBoardSolver();
        var request = new ShuffleAdviceRequest(new LineChances(0, 0, 0), 9, 9, ShuffleObjective.TwoLineMax);

        Assert.Equal(ShuffleRecommendation.Unavailable, solver.GetShuffleAdvice(request).Recommendation);
    }

    [Fact]
    public void GetShuffleAdvice_InsufficientPoints_IsNeedSecondChance() {
        var solver = new WondrousTailsBoardSolver();
        var cells = Board(0, 1, 2, 3, 4);
        var current = solver.CalculateLineChances(cells);
        var request = new ShuffleAdviceRequest(current, 5, 1, ShuffleObjective.TwoLineMax);

        var advice = solver.GetShuffleAdvice(request);

        Assert.Equal(ShuffleRecommendation.NeedSecondChance, advice.Recommendation);
        Assert.Equal(ShuffleObjective.TwoLineMax, advice.Objective);
    }

    [Fact]
    public void GetShuffleAdvice_CurrentBelowBaseline_RecommendsShuffle() {
        var solver = new WondrousTailsBoardSolver();
        var baseline = solver.GetShuffleBaseline(5)!.Value;
        // Current two-line chance well below the 5-sticker shuffle average, supplied as
        // a value the way the caller does. GetShuffleAdvice takes the chances as input,
        // so this isolates the verdict wiring from board-probability computation.
        var current = new LineChances(0, baseline.TwoLines - 0.05, 0);

        var request = new ShuffleAdviceRequest(current, 5, 2, ShuffleObjective.TwoLineMax);
        var advice = solver.GetShuffleAdvice(request);

        Assert.Equal(ShuffleRecommendation.Shuffle, advice.Recommendation);
        Assert.Equal(current, advice.CurrentChances);
        Assert.Equal(baseline, advice.Baseline);
    }

    [Theory]
    [InlineData((ShuffleObjective)999, ShuffleObjective.TwoLineMax)]
    [InlineData(ShuffleObjective.OneLineMax, ShuffleObjective.OneLineMax)]
    [InlineData(ShuffleObjective.ThreeLineMax, ShuffleObjective.ThreeLineMax)]
    public void Normalize_ClampsUndefinedToDefault(ShuffleObjective input, ShuffleObjective expected) {
        Assert.Equal(expected, ShuffleObjectives.Normalize(input));
    }
}
