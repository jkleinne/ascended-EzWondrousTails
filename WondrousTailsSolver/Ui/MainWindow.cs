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
    private const int BoardGridSize = 4;
    private const int StickerCountMaximum = 9;
    private const float MinimumWidth = 320;
    private const float MinimumHeight = 220;

    private readonly PerfectTails perfectTails;

    internal MainWindow(PerfectTails perfectTails) : base(Title) {
        this.perfectTails = perfectTails;

        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(MinimumWidth, MinimumHeight),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw() {
        var playerState = PlayerState.Instance();
        if (playerState is null || !playerState->HasWeeklyBingoJournal) {
            ImGui.TextWrapped("No active Wondrous Tails journal on this character.");
            return;
        }

        perfectTails.RefreshGameState();

        ImGui.TextUnformatted($"Stickers placed: {playerState->WeeklyBingoNumPlacedStickers}/{StickerCountMaximum}");
        ImGui.TextUnformatted($"Second Chance points: {playerState->WeeklyBingoNumSecondChancePoints}");
        ImGui.Separator();

        ImGui.TextWrapped(perfectTails.GetProbabilityText());
        ImGui.Spacing();
        ImGui.TextUnformatted("Board state");

        for (var row = 0; row < BoardGridSize; row++) {
            var line = string.Empty;
            for (var column = 0; column < BoardGridSize; column++) {
                var filled = perfectTails.GameState[(row * BoardGridSize) + column];
                line += (filled ? FilledCellGlyph : EmptyCellGlyph) + " ";
            }
            ImGui.TextUnformatted(line.TrimEnd());
        }
    }
}
