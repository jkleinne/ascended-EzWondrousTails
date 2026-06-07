using System;

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
    /// Returns the game's base instruction text: everything before the first marker,
    /// or the whole string when no marker is present (pristine or changed game text).
    /// </summary>
    public static string ExtractBaseText(string currentText, string marker) {
        var markerIndex = currentText.IndexOf(marker, StringComparison.Ordinal);
        return markerIndex >= 0 ? currentText[..markerIndex] : currentText;
    }

    private static readonly char[] LineBreakCharacters = ['\r', '\n'];

    /// <summary>
    /// Returns the first logical line of the instruction text: everything before
    /// the first carriage return or line feed, or the whole string when it has no
    /// line break. The game appends a variable, multi-line reminder to the
    /// instruction node when a line of seals is completed; keeping only the first
    /// line stops that reminder from displacing the plugin's injected output below
    /// the visible journal area. Splits on both '\r' and '\n' to match how the
    /// node text reads back.
    /// </summary>
    public static string KeepFirstLine(string baseText) {
        var breakIndex = baseText.IndexOfAny(LineBreakCharacters);
        return breakIndex >= 0 ? baseText[..breakIndex] : baseText;
    }
}
