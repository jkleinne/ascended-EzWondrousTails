using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace WondrousTailsSolver;

/// <summary>
/// Calculates Wondrous Tails line probabilities from board values so Dalamud
/// state and UI formatting stay outside the solver.
/// </summary>
public sealed class WondrousTailsBoardSolver {
    public const int CellCount = GridSize * GridSize;

    private const int GridSize = 4;
    private const int MaxStickers = 9;
    private const int ShuffleMinimumStickers = 3;
    private const int ShuffleMaximumStickers = 7;
    public const uint ShuffleSecondChanceCost = 2;
    private const int OutcomeCount = 4;
    private const double ShuffleThreshold = -0.005;
    private const double StrongKeepThreshold = 0.01;

    private readonly Dictionary<int, long[]> possibleBoards = [];
    private readonly Dictionary<int, LineChances> shuffleBaselines = [];

    /// <summary>
    /// Initializes the board cache once so per-frame display updates only perform
    /// dictionary lookups and lightweight formatting.
    /// </summary>
    public WondrousTailsBoardSolver() {
        CalculateBoards(0, 0, 0, 0, 0);
        CalculateShuffleBaselines();
    }

    /// <summary>
    /// Calculates final one, two, and three line chances for the supplied board.
    /// </summary>
    public LineChances CalculateLineChances(bool[] cells) {
        if (cells.Length != CellCount) {
            throw new ArgumentException($"Expected {CellCount} Wondrous Tails cells.", nameof(cells));
        }

        var mask = CellsToMask(cells);
        return CalculateLineChances(mask);
    }

    /// <summary>
    /// Returns the exact average line chances for all boards with the same
    /// sticker count, matching the set of boards a shuffle can produce.
    /// </summary>
    public LineChances? GetShuffleBaseline(int stickersPlaced)
        => shuffleBaselines.TryGetValue(stickersPlaced, out var baseline) ? baseline : null;

    /// <summary>
    /// Compares the current board against the exact shuffle baseline so the UI
    /// can explain whether spending Second Chance points is likely worthwhile.
    /// </summary>
    public ShuffleAdvice GetShuffleAdvice(bool[] cells, int stickersPlaced, uint secondChancePoints) {
        var baseline = GetShuffleBaseline(stickersPlaced);
        if (baseline is null) {
            return ShuffleAdvice.Unavailable;
        }

        if (secondChancePoints < ShuffleSecondChanceCost) {
            return new ShuffleAdvice(ShuffleRecommendation.NeedSecondChance, baseline.Value, 0);
        }

        var current = CalculateLineChances(cells);
        var threeLineDelta = current.ThreeLines - baseline.Value.ThreeLines;
        var recommendation = threeLineDelta switch {
            >= StrongKeepThreshold => ShuffleRecommendation.StrongKeep,
            >= 0 => ShuffleRecommendation.Keep,
            <= ShuffleThreshold => ShuffleRecommendation.Shuffle,
            _ => ShuffleRecommendation.Neutral,
        };

        return new ShuffleAdvice(recommendation, baseline.Value, threeLineDelta);
    }

    private LineChances CalculateLineChances(int mask) {
        if (!possibleBoards.TryGetValue(mask, out var counts) || counts[0] == 0) {
            return LineChances.Error;
        }

        var divisor = (double)counts[0];
        return new LineChances(
            Math.Round(counts[1] / divisor, 4),
            Math.Round(counts[2] / divisor, 4),
            Math.Round(counts[3] / divisor, 4));
    }

    private void CalculateShuffleBaselines() {
        for (var stickersPlaced = ShuffleMinimumStickers; stickersPlaced <= ShuffleMaximumStickers; stickersPlaced++) {
            var masks = Enumerable.Range(0, 1 << CellCount)
                .Where(mask => BitOperations.PopCount((uint)mask) == stickersPlaced)
                .ToArray();

            shuffleBaselines[stickersPlaced] = new LineChances(
                Math.Round(masks.Average(mask => CalculateLineChances(mask).OneLine), 4),
                Math.Round(masks.Average(mask => CalculateLineChances(mask).TwoLines), 4),
                Math.Round(masks.Average(mask => CalculateLineChances(mask).ThreeLines), 4));
        }
    }

    private long[] CalculateBoards(int mask, int numStickers, int numRows, int numCols, int numDiags) {
        if (possibleBoards.TryGetValue(mask, out var result)) {
            return result;
        }

        if (numStickers == MaxStickers) {
            var lines = numRows + numCols + numDiags;
            return possibleBoards[mask] = [
                1,
                lines >= 1 ? 1 : 0,
                lines >= 2 ? 1 : 0,
                lines >= 3 ? 1 : 0,
            ];
        }

        result = possibleBoards[mask] = new long[OutcomeCount];

        for (var row = 0; row < GridSize; row++) {
            for (var column = 0; column < GridSize; column++) {
                if (MaskHasBit(mask, row, column)) {
                    continue;
                }

                var nextMask = SetMaskBit(mask, row, column);
                var nextRows = MaskHasRow(nextMask, row) ? 1 : 0;
                var nextCols = MaskHasCol(nextMask, column) ? 1 : 0;
                var nextDiag1 = MaskHasDiag1(nextMask) && row == column ? 1 : 0;
                var nextDiag2 = MaskHasDiag2(nextMask) && row == GridSize - 1 - column ? 1 : 0;
                var nextResult = CalculateBoards(
                    nextMask,
                    numStickers + 1,
                    numRows + nextRows,
                    numCols + nextCols,
                    numDiags + nextDiag1 + nextDiag2);

                for (var outcomeIndex = 0; outcomeIndex < OutcomeCount; outcomeIndex++) {
                    result[outcomeIndex] += nextResult[outcomeIndex];
                }
            }
        }

        return result;
    }

    private static int CellsToMask(bool[] cells) {
        var mask = 0;
        for (var row = 0; row < GridSize; row++) {
            for (var column = 0; column < GridSize; column++) {
                if (cells[(row * GridSize) + column]) {
                    mask = SetMaskBit(mask, row, column);
                }
            }
        }

        return mask;
    }

    private static int GetMaskBit(int row, int column)
        => 1 << ((GridSize * row) + column);

    private static int SetMaskBit(int mask, int row, int column)
        => mask | GetMaskBit(row, column);

    private static bool MaskHasBit(int mask, int row, int column)
        => (mask & GetMaskBit(row, column)) == GetMaskBit(row, column);

    private static bool MaskHasRow(int mask, int row)
        => Enumerable.Range(0, GridSize).All(column => MaskHasBit(mask, row, column));

    private static bool MaskHasCol(int mask, int column)
        => Enumerable.Range(0, GridSize).All(row => MaskHasBit(mask, row, column));

    private static bool MaskHasDiag1(int mask)
        => Enumerable.Range(0, GridSize).All(index => MaskHasBit(mask, index, index));

    private static bool MaskHasDiag2(int mask)
        => Enumerable.Range(0, GridSize).All(index => MaskHasBit(mask, index, GridSize - 1 - index));
}

public readonly record struct LineChances(double OneLine, double TwoLines, double ThreeLines) {
    public static LineChances Error { get; } = new(-1, -1, -1);

    public double[] ToArray()
        => [OneLine, TwoLines, ThreeLines];
}

public readonly record struct ShuffleAdvice(
    ShuffleRecommendation Recommendation,
    LineChances Baseline,
    double ThreeLineDelta) {
    public static ShuffleAdvice Unavailable { get; } = new(ShuffleRecommendation.Unavailable, default, 0);
}

public enum ShuffleRecommendation {
    Unavailable,
    NeedSecondChance,
    Shuffle,
    Neutral,
    Keep,
    StrongKeep,
}
