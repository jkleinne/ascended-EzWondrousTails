using System;
using System.Collections.Generic;
using System.Linq;

namespace WondrousTailsSolver;

/// <summary>
/// Budget-aware keep/shuffle decision. Models a Wondrous Tails shuffle as an i.i.d. draw from the
/// distribution of objective scores over all boards with the same sticker count, then decides via
/// finite-horizon optimal stopping over the number of shuffles the player can afford. Pure: depends
/// only on injected score distributions and the reward-weight model, never on Dalamud or live state.
/// </summary>
public sealed class ShufflePolicy {
    // A shuffle re-randomizes the same stickers, so consecutive shuffles draw from the same
    // fixed-sticker-count distribution. Second Chance points cap at 9 in game and a shuffle costs
    // ShuffleSecondChanceCost, so a player can afford at most this many consecutive shuffles.
    private const int MaxSecondChancePoints = 9;
    private static readonly int MaxAffordableShuffles =
        MaxSecondChancePoints / (int)WondrousTailsBoardSolver.ShuffleSecondChanceCost;

    // Verdict bands scale to the score distribution's own spread (population standard deviation) so
    // they self-calibrate across objectives and sticker counts instead of assuming a fixed magnitude.
    // StrongKeep needs the current score at least a quarter sigma above the shuffle value; the neutral
    // band just below the shuffle value absorbs the opportunity cost of spending points on a marginal
    // gain (so a tiny EV edge does not trigger a shuffle).
    private const double StrongKeepBandSigmaFraction = 0.25;
    private const double NeutralBandSigmaFraction = 0.1;

    // Floor so a degenerate zero-variance distribution (unreachable for real 3-7 sticker geometries,
    // guarded regardless) cannot produce zero-width bands.
    private const double MinimumSigma = 1e-9;

    private readonly Dictionary<(ShuffleObjective Objective, int StickersPlaced), Thresholds> tables = [];

    /// <summary>
    /// Precomputes shuffle thresholds and spread for every objective and supplied sticker count from
    /// raw (unrounded) per-board chances, so per-frame evaluation is a dictionary lookup. The raw board
    /// data is consumed here and not retained.
    /// </summary>
    public ShufflePolicy(IReadOnlyDictionary<int, IReadOnlyList<LineChances>> rawChancesByStickerCount) {
        ArgumentNullException.ThrowIfNull(rawChancesByStickerCount);

        foreach (var (stickersPlaced, boards) in rawChancesByStickerCount) {
            foreach (var objective in Enum.GetValues<ShuffleObjective>()) {
                var scores = boards.Select(chances => chances.ScoreFor(objective)).ToArray();
                tables[(objective, stickersPlaced)] =
                    new Thresholds(ComputeShuffleThresholds(scores, MaxAffordableShuffles), StandardDeviation(scores));
            }
        }
    }

    /// <summary>
    /// Decides keep vs shuffle for the current board, accounting for how many shuffles the player can
    /// afford. Precondition: <paramref name="request"/>.CurrentChances is a valid (non-error) result.
    /// </summary>
    public ShuffleVerdict Evaluate(ShuffleAdviceRequest request) {
        if (!tables.TryGetValue((request.Objective, request.StickersPlaced), out var table)) {
            return ShuffleVerdict.Unavailable;
        }

        var affordableShuffles = Math.Min(
            MaxAffordableShuffles,
            (int)(request.SecondChancePoints / WondrousTailsBoardSolver.ShuffleSecondChanceCost));
        if (affordableShuffles < 1) {
            return ShuffleVerdict.NeedSecondChance;
        }

        var score = request.CurrentChances.ScoreFor(request.Objective);
        var shuffleValue = table.Thetas[affordableShuffles - 1];
        var recommendation = VerdictForGap(score - shuffleValue, table.Sigma);
        return new ShuffleVerdict(recommendation, affordableShuffles, shuffleValue - score);
    }

    /// <summary>
    /// Optimal-stopping shuffle values by affordable shuffle count. Index 0 is the value of a single
    /// shuffle (the distribution mean); each later entry is E[max(draw, previous value)] — the value of
    /// being able to shuffle again. Non-decreasing and bounded by the distribution maximum. Exposed for
    /// unit testing the recursion.
    /// </summary>
    public static double[] ComputeShuffleThresholds(IReadOnlyList<double> scores, int maxBudget) {
        if (scores.Count == 0) {
            throw new ArgumentException("Cannot compute shuffle thresholds from an empty distribution.", nameof(scores));
        }

        var thetas = new double[maxBudget];
        thetas[0] = scores.Average();
        for (var budget = 1; budget < maxBudget; budget++) {
            var previous = thetas[budget - 1];
            thetas[budget] = scores.Average(score => Math.Max(score, previous));
        }

        return thetas;
    }

    /// <summary>
    /// Maps the gap between the current score and the shuffle value to a recommendation, with bands
    /// scaled to the distribution spread. Exposed for unit testing the band boundaries.
    /// </summary>
    public static ShuffleRecommendation VerdictForGap(double gap, double sigma) {
        var band = Math.Max(sigma, MinimumSigma);
        if (gap >= StrongKeepBandSigmaFraction * band) {
            return ShuffleRecommendation.StrongKeep;
        }
        if (gap >= 0) {
            return ShuffleRecommendation.Keep;
        }
        if (gap > -NeutralBandSigmaFraction * band) {
            return ShuffleRecommendation.Neutral;
        }
        return ShuffleRecommendation.Shuffle;
    }

    private static double StandardDeviation(IReadOnlyList<double> scores) {
        var mean = scores.Average();
        var variance = scores.Average(score => (score - mean) * (score - mean));
        return Math.Sqrt(variance);
    }

    private readonly record struct Thresholds(double[] Thetas, double Sigma);
}

/// <summary>
/// Outcome of a shuffle evaluation. <see cref="AffordableShuffles"/> is shown to the player so the
/// budget-aware verdict is legible; <see cref="ExpectedRewardGain"/> (shuffle value minus current score)
/// is diagnostic only and never displayed, because reward units are not a percentage.
/// </summary>
public readonly record struct ShuffleVerdict(
    ShuffleRecommendation Recommendation,
    int AffordableShuffles,
    double ExpectedRewardGain) {
    public static ShuffleVerdict Unavailable { get; } = new(ShuffleRecommendation.Unavailable, 0, 0);
    public static ShuffleVerdict NeedSecondChance { get; } = new(ShuffleRecommendation.NeedSecondChance, 0, 0);
}
