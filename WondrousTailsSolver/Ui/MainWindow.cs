using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace WondrousTailsSolver.Ui;

/// <summary>
/// Standalone display of the current Wondrous Tails board and probability,
/// usable when the in-game Wondrous Tails window is not open.
/// </summary>
internal sealed unsafe class MainWindow : Window {
    private const string Title = "Wondrous Tails Solver";
    private const string FilledCellGlyph = "■";
    private const string EmptyCellGlyph = "□";

    public MainWindow() : base(Title) {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(320, 220),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw() {
        var playerState = PlayerState.Instance();
        if (playerState is null || !playerState->HasWeeklyBingoJournal) {
            ImGui.TextWrapped("No active Wondrous Tails journal on this character.");
            return;
        }

        System.PerfectTails.RefreshGameState();

        ImGui.TextUnformatted($"Stickers placed: {playerState->WeeklyBingoNumPlacedStickers}/9");
        ImGui.TextUnformatted($"Second Chance points: {playerState->WeeklyBingoNumSecondChancePoints}");
        ImGui.Separator();

        ImGui.TextWrapped(System.PerfectTails.GetProbabilityText());
        ImGui.Spacing();
        ImGui.TextUnformatted("Board state");

        for (var row = 0; row < 4; row++) {
            var line = string.Empty;
            for (var column = 0; column < 4; column++) {
                var filled = System.PerfectTails.GameState[(row * 4) + column];
                line += (filled ? FilledCellGlyph : EmptyCellGlyph) + " ";
            }
            ImGui.TextUnformatted(line.TrimEnd());
        }
    }
}
