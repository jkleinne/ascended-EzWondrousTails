using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WondrousTailsSolver;

/// <summary>
/// Listens to the Wondrous Tails addon lifecycle and injects probability
/// information into its instruction text node.
/// </summary>
public sealed unsafe class AddonWeeklyBingoController : IDisposable {
    private const string AddonName = "WeeklyBingo";

    // The instruction text node in the Wondrous Tails addon. Identified by
    // the original implementation (MidoriKami) and assumed stable; falling
    // back to a content scan would require client-language-aware matching.
    private const uint InstructionTextNodeId = 34;

    private const string ProbabilityPrefix = "Line Chances: ";
    private const string AveragePrefix = "Shuffle Average: ";
    private const string AdvicePrefix = "Shuffle Advice: ";

    // Fallback line spacing in pixels when the addon's text node reports zero.
    // 16px matches the addon's resolved font size in default UI scale.
    private const byte FallbackLineSpacing = 16;

    private string? capturedOriginalText;
    private ushort capturedHeight;
    private bool hasCapturedLayout;
    private bool disposed;

    public AddonWeeklyBingoController(IDalamudPluginInterface pluginInterface) {
        DalamudServices.Initialize(pluginInterface);

        DalamudServices.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, AddonName, OnAddonEvent);
        DalamudServices.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, AddonName, OnAddonEvent);
        DalamudServices.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, AddonName, OnAddonEvent);
        DalamudServices.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, AddonName, OnAddonEvent);
        DalamudServices.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, AddonName, OnAddonEvent);

        var current = GetOpenAddon();
        if (current is not null) {
            AddonRefresh(current);
        }
    }

    public void Dispose() {
        if (disposed) return;

        DalamudServices.AddonLifecycle.UnregisterListener(OnAddonEvent);

        var current = GetOpenAddon();
        if (current is not null) {
            RestoreOriginal(current);
        }

        ClearCapturedState();
        disposed = true;
    }

    private void OnAddonEvent(AddonEvent type, AddonArgs args) {
        var addon = (AddonWeeklyBingo*)args.Addon.Address;

        switch (type) {
            case AddonEvent.PreFinalize:
                // Mutating addon nodes during finalize is unsafe; drop captured state
                // so the next setup recaptures from a fresh node.
                ClearCapturedState();
                return;
            case AddonEvent.PostSetup:
            case AddonEvent.PostRefresh:
            case AddonEvent.PostRequestedUpdate:
            case AddonEvent.PostUpdate:
                AddonRefresh(addon);
                return;
        }
    }

    private void AddonRefresh(AddonWeeklyBingo* addon) {
        System.PerfectTails.RefreshGameState();
        UpdateInstructionText(addon);
    }

    private void UpdateInstructionText(AddonWeeklyBingo* addon) {
        var node = addon->GetTextNodeById(InstructionTextNodeId);
        if (node is null) return;

        var currentText = SeString.Parse(node->NodeText).TextValue;
        if (string.IsNullOrEmpty(currentText)) return;

        if (!hasCapturedLayout) {
            capturedOriginalText = currentText;
            capturedHeight = node->GetHeight();
            hasCapturedLayout = true;
        }

        node->TextFlags |= TextFlags.MultiLine;

        var baseText = StripPreviousInjection(currentText);
        var probability = System.PerfectTails.SolveAndGetProbabilitySeString();

        var builder = new SeStringBuilder();
        builder.AddText(baseText);
        builder.AddText("\r\r");
        builder.Append(probability);

        var lineSpacing = node->LineSpacing > 0 ? node->LineSpacing : FallbackLineSpacing;
        var injectedLines = CountLines(probability.TextValue) + 1;
        var desiredHeight = (ushort)(capturedHeight + (lineSpacing * injectedLines));
        if (node->GetHeight() != desiredHeight) {
            node->SetHeight(desiredHeight);
        }

        node->SetText(builder.Encode());
    }

    private void RestoreOriginal(AddonWeeklyBingo* addon) {
        if (!hasCapturedLayout || capturedOriginalText is null) return;

        var node = addon->GetTextNodeById(InstructionTextNodeId);
        if (node is null) return;

        node->SetText(capturedOriginalText);
        if (capturedHeight > 0) {
            node->SetHeight(capturedHeight);
        }
    }

    private void ClearCapturedState() {
        capturedOriginalText = null;
        capturedHeight = 0;
        hasCapturedLayout = false;
    }

    private static string StripPreviousInjection(string text) {
        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.StartsWith(ProbabilityPrefix, StringComparison.Ordinal)
                        && !line.StartsWith(AveragePrefix, StringComparison.Ordinal)
                        && !line.StartsWith(AdvicePrefix, StringComparison.Ordinal))
            .ToArray();
        return string.Join("\r", lines);
    }

    private static int CountLines(string text)
        => Math.Max(1, text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length);

    private static AddonWeeklyBingo* GetOpenAddon() {
        var address = DalamudServices.GameGui.GetAddonByName(AddonName).Address;
        return address == nint.Zero ? null : (AddonWeeklyBingo*)address;
    }
}
