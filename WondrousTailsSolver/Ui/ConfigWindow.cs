using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace WondrousTailsSolver.Ui;

/// <summary>
/// Plugin settings window for display preferences and current journal status.
/// </summary>
internal sealed unsafe class ConfigWindow : Window {
    private const string Title = "Wondrous Tails Solver — Settings";
    private const string DisplayOptionsHeading = "Display Options";
    private const string CurrentJournalHeading = "Current Journal";
    private const int StickerCountMaximum = 9;
    private const float MinimumWidth = 360;
    private const float MinimumHeight = 260;

    private static readonly string[] PrecisionLabels = [
        "0 decimals",
        "1 decimal",
        "2 decimals",
    ];

    private static readonly string[] ObjectiveLabels = [
        "1 line",
        "2 lines",
        "3 lines",
        "1 & 2 lines",
    ];

    private readonly PluginConfiguration configuration;
    private readonly Action saveConfiguration;
    private readonly PerfectTails perfectTails;

    internal ConfigWindow(
        PluginConfiguration configuration,
        Action saveConfiguration,
        PerfectTails perfectTails) : base(Title) {
        this.configuration = configuration;
        this.saveConfiguration = saveConfiguration;
        this.perfectTails = perfectTails;

        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(MinimumWidth, MinimumHeight),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw() {
        ImGui.TextUnformatted(DisplayOptionsHeading);
        DrawDisplayOptions();

        ImGui.Separator();
        ImGui.TextUnformatted(CurrentJournalHeading);
        DrawCurrentJournal();
    }

    private void DrawDisplayOptions() {
        DrawCheckbox("Enable journal overlay", configuration.EnableJournalOverlay, value => configuration.EnableJournalOverlay = value);
        DrawCheckbox("Show line chances", configuration.ShowLineChances, value => configuration.ShowLineChances = value);
        DrawCheckbox("Show shuffle average", configuration.ShowShuffleAverage, value => configuration.ShowShuffleAverage = value);
        DrawCheckbox("Show shuffle advice", configuration.ShowShuffleAdvice, value => configuration.ShowShuffleAdvice = value);
        DrawCheckbox("Use colored journal text", configuration.UseColoredJournalText, value => configuration.UseColoredJournalText = value);

        if (!configuration.HasAnyDisplaySectionEnabled) {
            ImGui.TextWrapped("Select at least one probability section to show journal overlay output.");
        }

        var selectedPrecision = configuration.ProbabilityDecimalPlaces;
        if (ImGui.Combo("Percentage precision", ref selectedPrecision, PrecisionLabels, PrecisionLabels.Length)) {
            configuration.DecimalPlaces = PluginConfiguration.FromDecimalPlaces(selectedPrecision);
            saveConfiguration();
        }

        if (configuration.ShowShuffleAdvice) {
            var selectedObjective = PluginConfiguration.ToObjectiveIndex(configuration.Objective);
            if (ImGui.Combo("Advice objective", ref selectedObjective, ObjectiveLabels, ObjectiveLabels.Length)) {
                configuration.Objective = PluginConfiguration.FromObjectiveIndex(selectedObjective);
                saveConfiguration();
            }
        }

        if (ImGui.Button("Reset defaults")) {
            configuration.ResetToDefaults();
            saveConfiguration();
        }
    }

    private void DrawCheckbox(string label, bool currentValue, Action<bool> setValue) {
        var value = currentValue;
        if (!ImGui.Checkbox(label, ref value)) return;

        setValue(value);
        saveConfiguration();
    }

    private void DrawCurrentJournal() {
        var playerState = PlayerState.Instance();
        if (playerState is null || !playerState->HasWeeklyBingoJournal) {
            ImGui.TextWrapped("No active Wondrous Tails journal on this character.");
            return;
        }

        perfectTails.RefreshGameState();

        ImGui.TextUnformatted($"Stickers placed: {playerState->WeeklyBingoNumPlacedStickers}/{StickerCountMaximum}");
        ImGui.TextUnformatted($"Second Chance points: {playerState->WeeklyBingoNumSecondChancePoints}");
        ImGui.Spacing();
        ImGui.TextWrapped(perfectTails.GetProbabilityText());
    }
}
