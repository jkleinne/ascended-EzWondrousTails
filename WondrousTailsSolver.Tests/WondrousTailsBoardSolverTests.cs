using System;
using WondrousTailsSolver;
using Xunit;

public class WondrousTailsBoardSolverTests {
    private static bool[] Board(params int[] filledIndices) {
        var cells = new bool[WondrousTailsBoardSolver.CellCount];
        foreach (var index in filledIndices) {
            cells[index] = true;
        }
        return cells;
    }

    [Fact]
    public void CalculateLineChances_TwoFullRows_ReturnsExactCumulativeOutcomes() {
        var solver = new WondrousTailsBoardSolver();
        // Rows 0 and 1 fully placed (indices 0-7) plus one more (index 8).
        // Exactly two completed lines, no third line possible at 9 stickers.
        var cells = Board(0, 1, 2, 3, 4, 5, 6, 7, 8);

        var chances = solver.CalculateLineChances(cells);

        Assert.Equal(1.0, chances.OneLine);
        Assert.Equal(1.0, chances.TwoLines);
        Assert.Equal(0.0, chances.ThreeLines);
    }

    [Fact]
    public void CalculateLineChances_WrongLength_Throws() {
        var solver = new WondrousTailsBoardSolver();
        Assert.Throws<ArgumentException>(() => solver.CalculateLineChances(new bool[3]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(9)]
    public void GetShuffleBaseline_OutOfShuffleRange_ReturnsNull(int stickers) {
        Assert.Null(new WondrousTailsBoardSolver().GetShuffleBaseline(stickers));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void GetShuffleBaseline_InRange_IsOrderedProbability(int stickers) {
        var baseline = new WondrousTailsBoardSolver().GetShuffleBaseline(stickers);

        Assert.NotNull(baseline);
        Assert.InRange(baseline!.Value.OneLine, 0.0, 1.0);
        Assert.InRange(baseline.Value.ThreeLines, 0.0, 1.0);
        Assert.True(baseline.Value.OneLine >= baseline.Value.TwoLines);
        Assert.True(baseline.Value.TwoLines >= baseline.Value.ThreeLines);
    }

    [Fact]
    public void CalculateLineChances_RoundsToAtMostFourDecimals() {
        var solver = new WondrousTailsBoardSolver();
        // A 5-sticker partial board yields fractional probabilities.
        var cells = Board(0, 1, 2, 5, 10);

        var chances = solver.CalculateLineChances(cells);

        Assert.Equal(System.Math.Round(chances.OneLine, 4), chances.OneLine);
        Assert.Equal(System.Math.Round(chances.TwoLines, 4), chances.TwoLines);
        Assert.Equal(System.Math.Round(chances.ThreeLines, 4), chances.ThreeLines);
    }
}
