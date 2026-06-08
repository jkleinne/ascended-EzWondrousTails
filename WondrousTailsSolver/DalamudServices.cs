using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace WondrousTailsSolver;

/// <summary>
/// Container for injected Dalamud services. Populated once at plugin load
/// via <see cref="IDalamudPluginInterface.Create{T}"/>.
/// </summary>
internal sealed class DalamudServices {
    private static bool initialized;

    /// <summary>
    /// Initializes the static service properties. Idempotent so that
    /// repeated calls are safe; only the first call performs injection.
    /// </summary>
    /// <param name="pluginInterface">Plugin interface supplied by Dalamud at load.</param>
    public static void Initialize(IDalamudPluginInterface pluginInterface) {
        if (initialized) {
            return;
        }

        pluginInterface.Create<DalamudServices>();
        initialized = true;
    }

    /// <summary>
    /// Lifecycle event source for in-game addons (windows). Used to hook
    /// the Wondrous Tails addon's setup, refresh, update, and finalize events
    /// without manually patching game functions.
    /// </summary>
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    /// <summary>
    /// Addon lookup service. Used to resolve the live Wondrous Tails addon
    /// pointer at plugin load time so the controller can attach to an
    /// already-open window without waiting for the next setup event.
    /// </summary>
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;

    /// <summary>
    /// Framework update loop access. Used to marshal native addon-node mutations
    /// onto the game's main thread. The plugin constructor and <see cref="IDisposable.Dispose"/>
    /// run on background load/unload threads, where touching a live AtkTextNode
    /// races the render thread and faults the game.
    /// </summary>
    [PluginService] public static IFramework Framework { get; private set; } = null!;

    /// <summary>
    /// Plugin log sink. Records faults raised inside the deferred framework-thread
    /// callbacks so that asynchronous work surfaces errors instead of swallowing them.
    /// </summary>
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
}
