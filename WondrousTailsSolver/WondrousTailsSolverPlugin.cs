using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using WondrousTailsSolver.Ui;

namespace WondrousTailsSolver;

public sealed class WondrousTailsSolverPlugin : IDalamudPlugin {
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly WindowSystem windowSystem = new("WondrousTailsSolver");
    private readonly MainWindow mainWindow = new();
    private readonly ConfigWindow configWindow = new();

    public WondrousTailsSolverPlugin(IDalamudPluginInterface pluginInterface) {
        this.pluginInterface = pluginInterface;

        System.PerfectTails = new PerfectTails();
        System.AddonWeeklyBingoController = new AddonWeeklyBingoController(pluginInterface);

        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(configWindow);

        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
    }

    public void Dispose() {
        pluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
        pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        windowSystem.RemoveAllWindows();
        System.AddonWeeklyBingoController.Dispose();
    }

    private void OpenMainUi() => mainWindow.IsOpen = true;

    private void OpenConfigUi() => configWindow.IsOpen = true;
}
