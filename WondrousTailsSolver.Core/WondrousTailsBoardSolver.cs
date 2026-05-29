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
    // Decision bands applied to (current score - shuffle-average score), in
    // probability units. The bands are small buffers around parity that bias toward
    // keeping near the average, because a shuffle costs 2 Second Chance points and is
    // not worth a marginal gain. Asymmetric on purpose: any positive edge keeps,
    // while a small negative edge stays Neutral rather than shuffling. Shared across
    // all objectives for now; the composite score swings about twice as hard, so the
    // bands are even more approximate for it (per-objective tuning is a future step).
    private const double ShuffleThreshold = -0.005;   // <= -0.5pp -> Shuffle
    private const double StrongKeepThreshold = 0.01;  // >= +1.0pp -> Strong keep

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
    /// Compares the current board against the exact shuffle baseline for the requested
    /// objective so the UI can explain whether spending Second Chance points is worthwhile.
    /// </summary>
    public ShuffleAdvice GetShuffleAdvice(ShuffleAdviceRequest request) {
        var baseline = GetShuffleBaseline(request.StickersPlaced);
        if (baseline is null) {
            return ShuffleAdvice.Unavailable;
        }

        if (request.SecondChancePoints < ShuffleSecondChanceCost) {
            return new ShuffleAdvice(
                ShuffleRecommendation.NeedSecondChance, request.CurrentChances, baseline.Value, request.Objective);
        }

        var delta = request.CurrentChances.ScoreFor(request.Objective) - baseline.Value.ScoreFor(request.Objective);
        return new ShuffleAdvice(
            RecommendationForDelta(delta), request.CurrentChances, baseline.Value, request.Objective);
    }

    /// <summary>
    /// Maps a (current - baseline) score delta to a keep/shuffle recommendation. Pure
    /// policy, exposed for unit testing the band boundaries.
    /// </summary>
    public static ShuffleRecommendation RecommendationForDelta(double delta) => delta switch {
        >= StrongKeepThreshold => ShuffleRecommendation.StrongKeep,
        >= 0 => ShuffleRecommendation.Keep,
        <= ShuffleThreshold => ShuffleRecommendation.Shuffle,
        _ => ShuffleRecommendation.Neutral,
    };

    private LineChances CalculateLineChances(int mask) {
        if (!possibleBoards.TryGetValue(mask, out var counts) || counts[0] == 0) {
            return LineChances.Error;
        }

        var raw = CalculateLineChancesRaw(counts);
        return new LineChances(
            Math.Round(raw.OneLine, 4),
            Math.Round(raw.TwoLines, 4),
            Math.Round(raw.ThreeLines, 4));
    }

    // Precondition: counts came from a board with counts[0] > 0 (a valid, in-range
    // mask). Returns unrounded probabilities so callers that aggregate (e.g. the
    // shuffle baseline) round exactly once at their own boundary and never average
    // in the LineChances.Error sentinel.
    private static LineChances CalculateLineChancesRaw(long[] counts) {
        var divisor = (double)counts[0];
        return new LineChances(
            counts[1] / divisor,
            counts[2] / divisor,
            counts[3] / divisor);
    }

    private void CalculateShuffleBaselines() {
        for (var stickersPlaced = ShuffleMinimumStickers; stickersPlaced <= ShuffleMaximumStickers; stickersPlaced++) {
            var raw = Enumerable.Range(0, 1 << CellCount)
                .Where(mask => BitOperations.PopCount((uint)mask) == stickersPlaced)
                .Select(mask => CalculateLineChancesRaw(possibleBoards[mask]))
                .ToArray();

            shuffleBaselines[stickersPlaced] = new LineChances(
                Math.Round(raw.Average(chances => chances.OneLine), 4),
                Math.Round(raw.Average(chances => chances.TwoLines), 4),
                Math.Round(raw.Average(chances => chances.ThreeLines), 4));
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

    /// <summary>
    /// The probability this objective optimizes. The composite values the first and
    /// second line equally (equal weight, no subjective reward weighting): it is the
    /// expected number of line-rewards among the first two, P(>=1) + P(>=2).
    /// </summary>
    public double ScoreFor(ShuffleObjective objective) => objective switch {
        ShuffleObjective.OneLineMax => OneLine,
        ShuffleObjective.TwoLineMax => TwoLines,
        ShuffleObjective.ThreeLineMax => ThreeLines,
        ShuffleObjective.OneAndTwoLineTradeoff => OneLine + TwoLines,
        _ => TwoLines,
    };
}

public readonly record struct ShuffleAdvice(
    ShuffleRecommendation Recommendation,
    LineChances CurrentChances,
    LineChances Baseline,
    ShuffleObjective Objective) {
    public static ShuffleAdvice Unavailable { get; } = new(ShuffleRecommendation.Unavailable, default, default, default);
}

/// <summary>
/// Inputs to a shuffle-advice query. <see cref="CurrentChances"/> is supplied by the
/// caller so line chances are computed once per refresh rather than recomputed here.
/// </summary>
public readonly record struct ShuffleAdviceRequest(
    LineChances CurrentChances,
    int StickersPlaced,
    uint SecondChancePoints,
    ShuffleObjective Objective);

public enum ShuffleRecommendation {
    Unavailable,
    NeedSecondChance,
    Shuffle,
    Neutral,
    Keep,
    StrongKeep,
}

/// <summary>
/// What the shuffle advice optimizes its keep/shuffle verdict on. Explicit values
/// keep persisted config integers stable against reordering; the default
/// (<see cref="ShuffleObjective.TwoLineMax"/>) is intentionally not the zero member.
/// </summary>
public enum ShuffleObjective {
    OneLineMax = 0,
    TwoLineMax = 1,
    ThreeLineMax = 2,
    OneAndTwoLineTradeoff = 3,
}
