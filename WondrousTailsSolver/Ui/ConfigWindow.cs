using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace WondrousTailsSolver.Ui;

/// <summary>
/// Plugin settings window. Currently a placeholder; the plugin's only
/// behavior is the addon text injection and there are no user-tunable
/// options for it yet.
/// </summary>
internal sealed class ConfigWindow : Window {
    private const string Title = "Wondrous Tails Solver — Settings";

    public ConfigWindow() : base(Title) {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(320, 120),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw() {
        ImGui.TextWrapped("No configurable options yet.");
    }
}
