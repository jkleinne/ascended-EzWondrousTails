using System;
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
internal sealed unsafe class AddonWeeklyBingoController : IDisposable {
    private const string AddonName = "WeeklyBingo";

    // The instruction text node in the Wondrous Tails addon. Identified by
    // the original implementation (MidoriKami) and assumed stable; falling
    // back to a content scan would require client-language-aware matching.
    private const uint InstructionTextNodeId = 34;

    private const string ProbabilityPrefix = "Line Chances: ";
    private const string AveragePrefix = "Shuffle Average: ";
    private const string AdvicePrefix = "Shuffle Advice: ";
    private const string ErrorPrefix = "Wondrous Tails Solver: ";
    private const string AdviceLineContinuation = "line)";
    private const string AdviceThreeLineContinuation = "3 line)";
    private const string AdviceDeltaFragment = "pp vs average";
    private const string SecondChanceContinuationFragment = "Second Chance points";
    private const string SecondChanceLineContinuation = "Chance points";

    // Fallback line spacing in pixels when the addon's text node reports zero.
    // 16px matches the addon's resolved font size in default UI scale.
    private const byte FallbackLineSpacing = 16;
    private const int SeparatorLineCount = 1;
    private const int MinimumLineCount = 1;
    private const byte NoLineSpacing = 0;
    private const ushort NoCapturedHeight = 0;

    private readonly PerfectTails perfectTails;
    private readonly PluginConfiguration configuration;
    private string? capturedOriginalText;
    private ushort capturedHeight;
    private TextFlags capturedTextFlags;
    private bool hasCapturedLayout;
    private bool disposed;

    internal AddonWeeklyBingoController(
        IDalamudPluginInterface pluginInterface,
        PerfectTails perfectTails,
        PluginConfiguration configuration) {
        this.perfectTails = perfectTails;
        this.configuration = configuration;

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
        if (configuration.EnableJournalOverlay && configuration.HasAnyDisplaySectionEnabled) {
            perfectTails.RefreshGameState();
        }

        UpdateInstructionText(addon);
    }

    private void UpdateInstructionText(AddonWeeklyBingo* addon) {
        var node = addon->GetTextNodeById(InstructionTextNodeId);
        if (node is null) return;

        var currentText = SeString.Parse(node->NodeText).TextValue;
        if (string.IsNullOrEmpty(currentText)) return;

        var baseText = StripPreviousInjection(currentText);

        if (!hasCapturedLayout) {
            capturedOriginalText = baseText;
            capturedHeight = node->GetHeight();
            capturedTextFlags = node->TextFlags;
            hasCapturedLayout = true;
        }

        if (!configuration.EnableJournalOverlay || !configuration.HasAnyDisplaySectionEnabled) {
            RestoreOriginal(addon);
            return;
        }

        node->TextFlags |= TextFlags.MultiLine;

        var probability = perfectTails.SolveAndGetProbabilitySeString();
        if (string.IsNullOrWhiteSpace(probability.TextValue)) {
            RestoreOriginal(addon);
            return;
        }

        var builder = new SeStringBuilder();
        builder.AddText(baseText);
        builder.AddText("\r\r");
        builder.Append(probability);

        var lineSpacing = node->LineSpacing > NoLineSpacing ? node->LineSpacing : FallbackLineSpacing;
        var injectedLines = CountLines(probability.TextValue) + SeparatorLineCount;
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
        node->TextFlags = capturedTextFlags;
        if (capturedHeight > NoCapturedHeight) {
            node->SetHeight(capturedHeight);
        }
    }

    private void ClearCapturedState() {
        capturedOriginalText = null;
        capturedHeight = NoCapturedHeight;
        capturedTextFlags = default;
        hasCapturedLayout = false;
    }

    private static string StripPreviousInjection(string text) {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        var injectionStart = Array.FindIndex(lines, IsInjectedLine);
        var baseLines = injectionStart >= 0 ? lines[..injectionStart] : lines;
        return string.Join("\r", TrimInjectedContinuationLines(baseLines));
    }

    private static string[] TrimInjectedContinuationLines(string[] lines) {
        var count = lines.Length;
        while (count > 0 && IsInjectedContinuationLine(lines[count - 1])) {
            count--;
        }

        return count == lines.Length ? lines : lines[..count];
    }

    private static bool IsInjectedLine(string line)
        => line.StartsWith(ProbabilityPrefix, StringComparison.Ordinal)
        || line.StartsWith(AveragePrefix, StringComparison.Ordinal)
        || line.StartsWith(AdvicePrefix, StringComparison.Ordinal)
        || line.StartsWith(ErrorPrefix, StringComparison.Ordinal);

    private static bool IsInjectedContinuationLine(string line) {
        var trimmedLine = line.Trim();
        return trimmedLine.Equals(AdviceLineContinuation, StringComparison.Ordinal)
            || trimmedLine.Equals(AdviceThreeLineContinuation, StringComparison.Ordinal)
            || trimmedLine.Contains(AdviceDeltaFragment, StringComparison.Ordinal)
            || trimmedLine.Contains(SecondChanceContinuationFragment, StringComparison.Ordinal)
            || trimmedLine.Equals(SecondChanceLineContinuation, StringComparison.Ordinal);
    }

    private static int CountLines(string text)
        => Math.Max(MinimumLineCount, text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length);

    private static AddonWeeklyBingo* GetOpenAddon() {
        var address = DalamudServices.GameGui.GetAddonByName(AddonName).Address;
        return address == nint.Zero ? null : (AddonWeeklyBingo*)address;
    }
}
