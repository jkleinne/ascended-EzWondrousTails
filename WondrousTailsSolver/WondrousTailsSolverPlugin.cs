using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using WondrousTailsSolver.Ui;

namespace WondrousTailsSolver;

/// <summary>
/// Dalamud plugin entry point that owns plugin lifetime, windows, and persisted configuration.
/// </summary>
public sealed class WondrousTailsSolverPlugin : IDalamudPlugin {
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly WindowSystem windowSystem = new("WondrousTailsSolver");
    private readonly PluginConfiguration configuration;
    private readonly PerfectTails perfectTails;
    private readonly AddonWeeklyBingoController addonWeeklyBingoController;
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;

    /// <summary>
    /// Initializes the plugin with Dalamud services, windows, and addon hooks.
    /// </summary>
    public WondrousTailsSolverPlugin(IDalamudPluginInterface pluginInterface) {
        this.pluginInterface = pluginInterface;

        configuration = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        if (configuration.Normalize()) {
            pluginInterface.SavePluginConfig(configuration);
        }

        var boardSolver = new WondrousTailsBoardSolver();
        var shufflePolicy = new ShufflePolicy(boardSolver.RawChancesByShuffleStickerCount());
        perfectTails = new PerfectTails(configuration, boardSolver, shufflePolicy);
        addonWeeklyBingoController = new AddonWeeklyBingoController(pluginInterface, perfectTails, configuration);
        mainWindow = new MainWindow(perfectTails);
        configWindow = new ConfigWindow(configuration, SaveConfiguration, perfectTails);

        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(configWindow);

        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
    }

    /// <summary>
    /// Releases Dalamud subscriptions and restores any modified game UI state.
    /// </summary>
    public void Dispose() {
        pluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
        pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        windowSystem.RemoveAllWindows();
        addonWeeklyBingoController.Dispose();
    }

    private void OpenMainUi() => mainWindow.IsOpen = true;

    private void OpenConfigUi() => configWindow.IsOpen = true;

    private void SaveConfiguration() {
        configuration.Normalize();
        pluginInterface.SavePluginConfig(configuration);
    }
}
