using System;
using System.Linq;

namespace WondrousTailsSolver;

/// <summary>
/// Pure helpers for locating the plugin's injected text within the Wondrous Tails
/// instruction node. The injection is delimited by an invisible marker so cleanup
/// is independent of the injected wording, language, and any word-wrap breaks the
/// game inserts on read-back.
/// </summary>
public static class JournalInjection {
    /// <summary>
    /// Zero-width space (U+200B) marking the boundary between the game's instruction
    /// text and the plugin's injected output. Written as an escape, never a raw glyph.
    /// </summary>
    public const string InjectionMarker = "\u200b";

    /// <summary>
    /// Stable heading for line probability output. Shared by formatting and parsing
    /// so stale markerless plugin output can be recognized without wording drift.
    /// </summary>
    public const string LineChancesLabel = "Line Chances:";

    /// <summary>
    /// Stable heading for shuffle baseline output. Shared by formatting and parsing
    /// so stale markerless plugin output can be recognized without wording drift.
    /// </summary>
    public const string ShuffleAverageLabel = "Shuffle Average:";

    /// <summary>
    /// Stable heading prefix for shuffle recommendation output. Shared by formatting
    /// and parsing because the rendered heading includes objective details.
    /// </summary>
    public const string ShuffleAdviceLabel = "Shuffle Advice";

    private static readonly string[] PluginSectionLabels = [
        LineChancesLabel,
        ShuffleAverageLabel,
        ShuffleAdviceLabel,
    ];

    /// <summary>
    /// Returns the game's base instruction text: everything before the first marker,
    /// everything before stale markerless plugin output, or the whole string when
    /// only game-owned text is present.
    /// </summary>
    public static string ExtractBaseText(string currentText, string marker) {
        var markerIndex = currentText.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex >= 0) {
            return currentText[..markerIndex];
        }

        var pluginSectionIndex = FirstPluginSectionIndex(currentText);
        return pluginSectionIndex >= 0
            ? currentText[..pluginSectionIndex].TrimEnd('\r', '\n')
            : currentText;
    }

    /// <summary>
    /// Returns whether text has plugin-owned output but lost its invisible marker,
    /// which can happen when the live addon node is refreshed from its current text.
    /// </summary>
    public static bool HasStalePluginOutputWithoutMarker(string currentText, string marker) {
        if (currentText.Contains(marker, StringComparison.Ordinal)) {
            return false;
        }

        return FirstPluginSectionIndex(currentText) >= 0;
    }

    /// <summary>
    /// Returns whether the current node text is game-owned text that should replace
    /// the captured base text and layout before overlay injection.
    /// </summary>
    public static bool ShouldCaptureGameText(
        string currentText,
        string baseText,
        string? capturedGameText,
        string marker) {
        if (currentText.Contains(marker, StringComparison.Ordinal)) {
            return false;
        }

        if (HasStalePluginOutputWithoutMarker(currentText, marker)) {
            return false;
        }

        if (capturedGameText is null) {
            return true;
        }

        return !string.Equals(baseText, capturedGameText, StringComparison.Ordinal);
    }

    private static int FirstPluginSectionIndex(string currentText) {
        var sectionIndexes = PluginSectionLabels
            .Select(label => PluginSectionIndex(currentText, label))
            .Where(index => index >= 0);

        return sectionIndexes.DefaultIfEmpty(-1).Min();
    }

    private static int PluginSectionIndex(string currentText, string label) {
        var searchStart = 0;
        while (searchStart < currentText.Length) {
            var index = currentText.IndexOf(label, searchStart, StringComparison.Ordinal);
            if (index < 0) {
                return -1;
            }

            if (IsSectionStart(currentText, index)) {
                return index;
            }

            searchStart = index + label.Length;
        }

        return -1;
    }

    private static bool IsSectionStart(string text, int index)
        => index == 0 || text[index - 1] is '\r' or '\n';
}
