using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace WondrousTailsSolver;

/// <summary>
/// Minigame solver.
/// </summary>
internal sealed partial class PerfectTails {
    private const double NoChance = 0;
    private const double FullChance = 1;
    private const double FullChanceTolerance = 0.1;
    private const double SampleBound = 0.05;
    private const ushort GoodColor = 67;
    private const ushort NeutralColor = 66;
    private const ushort WarningColor = 561;
    private const ushort ErrorColor = 704;
    private const ushort StrongGlowColor = 2;

    private readonly PluginConfiguration configuration;
    private readonly WondrousTailsBoardSolver boardSolver;
    private readonly ShufflePolicy shufflePolicy;

    internal readonly bool[] GameState = new bool[WondrousTailsBoardSolver.CellCount];

    internal PerfectTails(PluginConfiguration configuration, WondrousTailsBoardSolver boardSolver, ShufflePolicy shufflePolicy) {
        this.configuration = configuration;
        this.boardSolver = boardSolver;
        this.shufflePolicy = shufflePolicy;
    }
}

/// <summary>
/// Getting formatted results
/// </summary>
internal sealed unsafe partial class PerfectTails {
    private const string EmptyConfiguredOutputText = "No configured probability sections are available for this board state.";
    private const string ErrorPrefix = "Wondrous Tails Solver: ";

    /// <summary>
    /// Refreshes <see cref="GameState"/> from the live player state. Call before
    /// invoking <see cref="SolveAndGetProbabilitySeString"/> or
    /// <see cref="GetProbabilityText"/> from a context that isn't already syncing
    /// state (e.g. the plugin's main window).
    /// </summary>
    internal bool RefreshGameState() {
        var playerState = PlayerState.Instance();
        if (playerState is null) {
            return false;
        }

        for (var index = 0; index < WondrousTailsBoardSolver.CellCount; index++) {
            GameState[index] = playerState->IsWeeklyBingoStickerPlaced(index);
        }

        return true;
    }

    /// <summary>
    /// Plain-text probability output for ImGui display. SeString carriage
    /// returns are normalized to line feeds so ImGui wraps cleanly.
    /// </summary>
    internal string GetProbabilityText() {
        var text = SolveAndGetProbabilitySeString().TextValue.Replace('\r', '\n');
        return string.IsNullOrWhiteSpace(text) ? EmptyConfiguredOutputText : text;
    }

    internal SeString SolveAndGetProbabilitySeString() {
        if (!configuration.HasAnyDisplaySectionEnabled) {
            return new SeStringBuilder().Build();
        }

        var playerState = PlayerState.Instance();
        if (playerState is null) {
            return BuildErrorSeString();
        }

        var stickersPlaced = playerState->WeeklyBingoNumPlacedStickers;
        var secondChancePoints = playerState->WeeklyBingoNumSecondChancePoints;
        var values = boardSolver.CalculateLineChances(this.GameState);

        if (values == LineChances.Error) {
            return BuildErrorSeString();
        }

        var baseline = boardSolver.GetShuffleBaseline(stickersPlaced);
        var valuePayloads = StringFormatDoubles(values.ToArray());
        var seString = new SeStringBuilder();
        var hasPreviousSection = false;

        if (configuration.ShowLineChances) {
            AppendSectionBreak(seString, ref hasPreviousSection);
            seString.AddText("Line Chances: ");
            AppendLineChances(seString, values, baseline, valuePayloads);
        }

        if (baseline is { } shuffleBaseline && configuration.ShowShuffleAverage) {
            AppendSectionBreak(seString, ref hasPreviousSection);
            seString.AddText("Shuffle Average: ");
            seString.AddText(string.Join(" ", StringFormatDoubles(shuffleBaseline.ToArray())));
        }

        if (baseline is { } adviceBaseline && configuration.ShowShuffleAdvice) {
            AppendSectionBreak(seString, ref hasPreviousSection);
            var request = new ShuffleAdviceRequest(values, stickersPlaced, secondChancePoints, configuration.Objective);
            var verdict = shufflePolicy.Evaluate(request);
            AppendShuffleAdvice(seString, verdict, values, adviceBaseline, configuration.Objective);
        }

        return seString.Build();
    }

    private void AppendLineChances(
        SeStringBuilder seString,
        LineChances values,
        LineChances? baseline,
        string[] valuePayloads) {
        if (baseline is { } shuffleBaseline) {
            var baselineValues = shuffleBaseline.ToArray();
            var chanceValues = values.ToArray();
            for (var index = 0; index < chanceValues.Length; index++) {
                var value = chanceValues[index];
                var sample = baselineValues[index];
                var valuePayload = valuePayloads[index];
                var sampleBoundLower = Math.Max(NoChance, sample - SampleBound);

                if (Math.Abs(value - FullChance) < FullChanceTolerance) {
                    AddGlowOrText(seString, valuePayload, StrongGlowColor);
                }
                else if (value < FullChance && value >= sample) {
                    AddForegroundOrText(seString, valuePayload, GoodColor);
                }
                else if (sample > value && value > sampleBoundLower) {
                    AddForegroundOrText(seString, valuePayload, NeutralColor);
                }
                else if (sampleBoundLower > value && value > NoChance) {
                    AddForegroundOrText(seString, valuePayload, WarningColor);
                }
                else if (value == NoChance) {
                    AddForegroundOrText(seString, valuePayload, ErrorColor);
                }
                else {
                    seString.AddText(valuePayload);
                }

                seString.AddText("  ");
            }
        }
        else {
            seString.AddText(string.Join(" ", valuePayloads));
        }
    }

    private string[] StringFormatDoubles(IEnumerable<double> values)
        => values.Select(FormatPercentage).ToArray();

    private string FormatPercentage(double value)
        => (value * 100).ToString($"F{configuration.ProbabilityDecimalPlaces}", CultureInfo.InvariantCulture) + "%";

    private SeString BuildErrorSeString() {
        var seString = new SeStringBuilder()
            .AddText(ErrorPrefix);

        AddForegroundOrText(seString, "error", ErrorColor);

        return seString.Build();
    }

    private void AppendShuffleAdvice(
        SeStringBuilder seString,
        ShuffleVerdict verdict,
        LineChances current,
        LineChances baseline,
        ShuffleObjective objective) {
        var label = ObjectiveLabel(objective);

        switch (verdict.Recommendation) {
            case ShuffleRecommendation.NeedSecondChance:
                seString.AddText($"Shuffle Advice ({label}): ");
                AddForegroundOrText(seString, $"need {WondrousTailsBoardSolver.ShuffleSecondChanceCost} Second Chance points", NeutralColor);
                return;
            case ShuffleRecommendation.Unavailable:
                seString.AddText($"Shuffle Advice ({label}): ");
                AddForegroundOrText(seString, "unavailable", ErrorColor);
                return;
        }

        seString.AddText($"Shuffle Advice ({label}, {FormatShuffleCount(verdict.AffordableShuffles)}): ");
        switch (verdict.Recommendation) {
            case ShuffleRecommendation.Shuffle:
                AddForegroundOrText(seString, "Shuffle", WarningColor);
                break;
            case ShuffleRecommendation.Neutral:
                AddForegroundOrText(seString, "Neutral", NeutralColor);
                break;
            case ShuffleRecommendation.Keep:
                AddForegroundOrText(seString, "Keep", GoodColor);
                break;
            case ShuffleRecommendation.StrongKeep:
                AddGlowOrText(seString, "Strong keep", StrongGlowColor);
                break;
        }

        seString.AddText($" ({FormatObjectiveDelta(current, baseline, objective)})");
    }

    private static string FormatShuffleCount(int affordableShuffles)
        => affordableShuffles == 1 ? "1 shuffle" : $"{affordableShuffles} shuffles";

    private static string ObjectiveLabel(ShuffleObjective objective) => objective switch {
        ShuffleObjective.OneLineMax => "1 line",
        ShuffleObjective.TwoLineMax => "2 lines",
        ShuffleObjective.ThreeLineMax => "3 lines",
        ShuffleObjective.OneAndTwoLineTradeoff => "1 & 2 lines",
        ShuffleObjective.RewardBalanced => "Reward balanced",
        _ => "2 lines",
    };

    private string FormatObjectiveDelta(LineChances current, LineChances baseline, ShuffleObjective objective) {
        var oneLine = FormatPercentagePointDelta(current.OneLine - baseline.OneLine);
        var twoLine = FormatPercentagePointDelta(current.TwoLines - baseline.TwoLines);
        var threeLine = FormatPercentagePointDelta(current.ThreeLines - baseline.ThreeLines);
        return objective switch {
            ShuffleObjective.OneLineMax => $"{oneLine} 1 line",
            ShuffleObjective.TwoLineMax => $"{twoLine} 2 line",
            ShuffleObjective.ThreeLineMax => $"{threeLine} 3 line",
            ShuffleObjective.OneAndTwoLineTradeoff => $"{oneLine} 1 line, {twoLine} 2 line",
            ShuffleObjective.RewardBalanced => $"{oneLine} 1 line, {twoLine} 2 line, {threeLine} 3 line",
            _ => $"{twoLine} 2 line",
        };
    }

    private string FormatPercentagePointDelta(double value) {
        var decimalPlaces = configuration.ProbabilityDecimalPlaces;
        var format = decimalPlaces == 0
            ? "+0;-0;0"
            : $"+0.{new string('0', decimalPlaces)};-0.{new string('0', decimalPlaces)};0.{new string('0', decimalPlaces)}";
        return (value * 100).ToString(format, CultureInfo.InvariantCulture) + "pp";
    }

    private void AddForegroundOrText(SeStringBuilder seString, string text, ushort color) {
        if (configuration.UseColoredJournalText) {
            seString.AddUiForeground(text, color);
            return;
        }

        seString.AddText(text);
    }

    private void AddGlowOrText(SeStringBuilder seString, string text, ushort color) {
        if (configuration.UseColoredJournalText) {
            seString.AddUiGlow(text, color);
            return;
        }

        seString.AddText(text);
    }

    private static void AppendSectionBreak(SeStringBuilder seString, ref bool hasPreviousSection) {
        if (hasPreviousSection) {
            seString.AddText("\r");
            return;
        }

        hasPreviousSection = true;
    }
}
