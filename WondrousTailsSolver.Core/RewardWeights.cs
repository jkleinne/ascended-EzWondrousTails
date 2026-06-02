namespace WondrousTailsSolver;

/// <summary>
/// Per-line-tier reward weights, applied to exact-tier line probabilities to produce an
/// expected-reward score. This is the single home for objective valuation, kept out of the
/// board solver so the solver stays weight-agnostic and the weight vectors are unit-testable.
/// </summary>
public readonly record struct RewardWeights(double OneLineWeight, double TwoLineWeight, double ThreeLineWeight) {
    // (1,3,9): geometric escalation ("each line ~3x the previous"), monotonic with increasing
    // marginals, and 3-line weighted above 2-line. Tunable; validated against sane verdicts in
    // tests rather than asserted as a measured reward table (real turn-in rewards are
    // heterogeneous and partly player-chosen, so no objective magnitude exists).
    private static readonly RewardWeights RewardBalancedWeights = new(1, 3, 9);

    /// <summary>
    /// Expected reward for a board: weights applied to exact-tier probabilities derived from the
    /// cumulative <see cref="LineChances"/> (P(=1)=P(&gt;=1)-P(&gt;=2), P(=2)=P(&gt;=2)-P(&gt;=3), P(=3)=P(&gt;=3)).
    /// A 0-line outcome scores 0. Consumes unrounded chances so callers that aggregate stay on one scale.
    /// </summary>
    public double ExpectedReward(LineChances cumulative) {
        var exactlyOneLine = cumulative.OneLine - cumulative.TwoLines;
        var exactlyTwoLines = cumulative.TwoLines - cumulative.ThreeLines;
        var exactlyThreeLines = cumulative.ThreeLines;
        return (OneLineWeight * exactlyOneLine)
            + (TwoLineWeight * exactlyTwoLines)
            + (ThreeLineWeight * exactlyThreeLines);
    }

    /// <summary>
    /// The weight vector for an objective. The four original objectives map to the vectors that
    /// reproduce their cumulative scores exactly (1-line=(1,1,1), 2-line=(0,1,1), 3-line=(0,0,1),
    /// 1&amp;2 tradeoff=(1,2,2)); unknown values fall back to the 2-line default.
    /// </summary>
    public static RewardWeights For(ShuffleObjective objective) => objective switch {
        ShuffleObjective.OneLineMax => new RewardWeights(1, 1, 1),
        ShuffleObjective.TwoLineMax => new RewardWeights(0, 1, 1),
        ShuffleObjective.ThreeLineMax => new RewardWeights(0, 0, 1),
        ShuffleObjective.OneAndTwoLineTradeoff => new RewardWeights(1, 2, 2),
        ShuffleObjective.RewardBalanced => RewardBalancedWeights,
        _ => new RewardWeights(0, 1, 1),
    };
}
