using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace WondrousTailsSolver;

/// <summary>
/// Minigame solver.
/// </summary>
public sealed partial class PerfectTails {
    private const double SampleBound = 0.05;
    private const int GoodColor = 67;
    private const int NeutralColor = 66;
    private const int WarningColor = 561;
    private const int ErrorColor = 704;
    private const int StrongGlowColor = 2;

    private readonly WondrousTailsBoardSolver boardSolver = new();

    public readonly bool[] GameState = new bool[WondrousTailsBoardSolver.CellCount];
}

/// <summary>
/// Getting formatted results
/// </summary>
public sealed unsafe partial class PerfectTails {
    /// <summary>
    /// Refreshes <see cref="GameState"/> from the live player state. Call before
    /// invoking <see cref="SolveAndGetProbabilitySeString"/> or
    /// <see cref="GetProbabilityText"/> from a context that isn't already syncing
    /// state (e.g. the plugin's main window).
    /// </summary>
    public void RefreshGameState() {
        for (var index = 0; index < WondrousTailsBoardSolver.CellCount; index++) {
            GameState[index] = PlayerState.Instance()->IsWeeklyBingoStickerPlaced(index);
        }
    }

    /// <summary>
    /// Plain-text probability output for ImGui display. SeString carriage
    /// returns are normalized to line feeds so ImGui wraps cleanly.
    /// </summary>
    public string GetProbabilityText()
        => SolveAndGetProbabilitySeString().TextValue.Replace('\r', '\n');

    public SeString SolveAndGetProbabilitySeString() {
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
        var valuePayloads = this.StringFormatDoubles(values.ToArray());
        var seString = new SeStringBuilder()
            .AddText("Line Chances: ");

        if (baseline is { } shuffleBaseline) {
            var baselineValues = shuffleBaseline.ToArray();
            var chanceValues = values.ToArray();
            foreach (var (value, sample, valuePayload) in Enumerable.Range(0, chanceValues.Length).Select(i => (chanceValues[i], baselineValues[i], valuePayloads[i]))) {
                var sampleBoundLower = Math.Max(0, sample - SampleBound);

                if (Math.Abs(value - 1) < 0.1f) {
                    seString.AddUiGlow(valuePayload, StrongGlowColor);
                }
                else if (value < 1 && value >= sample) {
                    seString.AddUiForeground(valuePayload, GoodColor);
                }
                else if (sample > value && value > sampleBoundLower) {
                    seString.AddUiForeground(valuePayload, NeutralColor);
                }
                else if (sampleBoundLower > value && value > 0) {
                    seString.AddUiForeground(valuePayload, WarningColor);
                }
                else if (value == 0) {
                    seString.AddUiForeground(valuePayload, ErrorColor);
                }
                else {
                    seString.AddText(valuePayload);
                }

                seString.AddText("  ");
            }

            seString.AddText("\rShuffle Average: ");
            seString.AddText(string.Join(" ", this.StringFormatDoubles(baselineValues)));
            AppendShuffleAdvice(seString, boardSolver.GetShuffleAdvice(this.GameState, stickersPlaced, secondChancePoints));
        }
        else {
            seString.AddText(string.Join(" ", valuePayloads));
        }
        
        return seString.Build();
    }

    private string[] StringFormatDoubles(IEnumerable<double> values)
        => values.Select(v => $"{v * 100:F2}%").ToArray();

    private static SeString BuildErrorSeString()
        => new SeStringBuilder()
            .AddText("Line Chances: ")
            .AddUiForeground("error ", ErrorColor)
            .AddUiForeground("error ", ErrorColor)
            .AddUiForeground("error ", ErrorColor)
            .Build();

    private static void AppendShuffleAdvice(SeStringBuilder seString, ShuffleAdvice advice) {
        seString.AddText("\rShuffle Advice: ");

        switch (advice.Recommendation) {
            case ShuffleRecommendation.NeedSecondChance:
                seString.AddUiForeground("need 2 Second Chance points", NeutralColor);
                return;
            case ShuffleRecommendation.Shuffle:
                seString.AddUiForeground("Shuffle", WarningColor);
                break;
            case ShuffleRecommendation.Neutral:
                seString.AddUiForeground("Neutral", NeutralColor);
                break;
            case ShuffleRecommendation.Keep:
                seString.AddUiForeground("Keep", GoodColor);
                break;
            case ShuffleRecommendation.StrongKeep:
                seString.AddUiGlow("Strong keep", StrongGlowColor);
                break;
            case ShuffleRecommendation.Unavailable:
            default:
                seString.AddUiForeground("unavailable", ErrorColor);
                return;
        }

        seString.AddText($" ({FormatPercentagePointDelta(advice.ThreeLineDelta)} 3 line)");
    }

    private static string FormatPercentagePointDelta(double value)
        => $"{value * 100:+0.00;-0.00;0.00}pp vs average";
}
