using WondrousTailsSolver;
using Xunit;

public class JournalInjectionTests {
    private const string Marker = JournalInjection.InjectionMarker;

    [Fact]
    public void ExtractBaseText_NoMarker_ReturnsWholeString() {
        Assert.Equal("Place stickers to fill the journal.",
            JournalInjection.ExtractBaseText("Place stickers to fill the journal.", Marker));
    }

    [Fact]
    public void ExtractBaseText_WithMarker_ReturnsTextBeforeMarker() {
        var text = "Base instruction." + Marker + "\r\r" + JournalInjection.LineChancesLabel + " 50% 25% 1%";
        Assert.Equal("Base instruction.", JournalInjection.ExtractBaseText(text, Marker));
    }

    [Fact]
    public void ExtractBaseText_WrapBreaksAfterMarker_StillReturnsBase() {
        var text = "Base." + Marker + JournalInjection.ShuffleAdviceLabel + " (2 lines): Keep (+0.50pp\n2 line)";
        Assert.Equal("Base.", JournalInjection.ExtractBaseText(text, Marker));
    }

    [Fact]
    public void ExtractBaseText_PluginLabelBeforeMarker_ReturnsTextBeforeMarker() {
        var text = "Base " + JournalInjection.LineChancesLabel + " still game text"
            + Marker
            + "\r\r"
            + JournalInjection.ShuffleAdviceLabel + " (2 lines): Keep";

        Assert.Equal("Base " + JournalInjection.LineChancesLabel + " still game text",
            JournalInjection.ExtractBaseText(text, Marker));
    }

    [Fact]
    public void ExtractBaseText_MarkerlessLineChanceOutput_ReturnsCompletedLineText() {
        var text = "Complete a line to receive a reward.\r\r" + JournalInjection.LineChancesLabel + " 100% 25% 1%";

        Assert.Equal("Complete a line to receive a reward.", JournalInjection.ExtractBaseText(text, Marker));
    }

    [Fact]
    public void ExtractBaseText_MarkerlessMultiplePluginLabels_StripsAtEarliestLabel() {
        var text = "Complete a line to receive a reward.\r\r"
            + JournalInjection.ShuffleAverageLabel + " 12% 3% 1%\r"
            + JournalInjection.LineChancesLabel + " 100% 25% 1%";

        Assert.Equal("Complete a line to receive a reward.", JournalInjection.ExtractBaseText(text, Marker));
    }

    [Fact]
    public void ExtractBaseText_MarkerlessCleanCompletedLineText_ReturnsWholeString() {
        Assert.Equal("Complete a line to receive a reward.",
            JournalInjection.ExtractBaseText("Complete a line to receive a reward.", Marker));
    }

    [Fact]
    public void ExtractBaseText_MarkerlessInlinePluginLabelText_ReturnsWholeString() {
        const string text = "A localized hint mentions Line Chances: as ordinary text.";

        Assert.Equal(text, JournalInjection.ExtractBaseText(text, Marker));
    }

    [Fact]
    public void HasStalePluginOutputWithoutMarker_MarkerlessPluginOutput_ReturnsTrue() {
        var text = "Complete a line to receive a reward.\r\r" + JournalInjection.ShuffleAdviceLabel + " (2 lines): Keep";

        Assert.True(JournalInjection.HasStalePluginOutputWithoutMarker(text, Marker));
    }

    [Fact]
    public void HasStalePluginOutputWithoutMarker_MarkerPresent_ReturnsFalse() {
        var text = "Complete a line to receive a reward." + Marker + "\r\r" + JournalInjection.LineChancesLabel + " 100%";

        Assert.False(JournalInjection.HasStalePluginOutputWithoutMarker(text, Marker));
    }

    [Fact]
    public void HasStalePluginOutputWithoutMarker_CleanCompletedLineText_ReturnsFalse() {
        Assert.False(JournalInjection.HasStalePluginOutputWithoutMarker(
            "Complete a line to receive a reward.",
            Marker));
    }

    [Fact]
    public void HasStalePluginOutputWithoutMarker_InlinePluginLabelText_ReturnsFalse() {
        Assert.False(JournalInjection.HasStalePluginOutputWithoutMarker(
            "A localized hint mentions Shuffle Advice as ordinary text.",
            Marker));
    }

    [Fact]
    public void ShouldCaptureGameText_NoCapturedText_ReturnsTrue() {
        Assert.True(JournalInjection.ShouldCaptureGameText(
            "Place stickers to fill the journal.",
            "Place stickers to fill the journal.",
            null,
            Marker));
    }

    [Fact]
    public void ShouldCaptureGameText_MarkerAbsentChangedText_ReturnsTrue() {
        Assert.True(JournalInjection.ShouldCaptureGameText(
            "Complete a line to receive a reward.",
            "Complete a line to receive a reward.",
            "Place stickers to fill the journal.",
            Marker));
    }

    [Fact]
    public void ShouldCaptureGameText_MarkerAbsentUnchangedText_ReturnsFalse() {
        Assert.False(JournalInjection.ShouldCaptureGameText(
            "Place stickers to fill the journal.",
            "Place stickers to fill the journal.",
            "Place stickers to fill the journal.",
            Marker));
    }

    [Fact]
    public void ShouldCaptureGameText_MarkerPresent_ReturnsFalse() {
        var text = "Place stickers to fill the journal." + Marker + "\r\r" + JournalInjection.LineChancesLabel + " 50% 25% 1%";

        Assert.False(JournalInjection.ShouldCaptureGameText(
            text,
            "Place stickers to fill the journal.",
            "Place stickers to fill the journal.",
            Marker));
    }

    [Fact]
    public void ShouldCaptureGameText_MarkerPresentWithoutCapturedText_ReturnsFalse() {
        var text = "Place stickers to fill the journal." + Marker + "\r\r" + JournalInjection.LineChancesLabel + " 50% 25% 1%";

        Assert.False(JournalInjection.ShouldCaptureGameText(
            text,
            "Place stickers to fill the journal.",
            null,
            Marker));
    }

    [Fact]
    public void ShouldCaptureGameText_MarkerlessStalePluginOutput_ReturnsFalse() {
        var text = "Complete a line to receive a reward.\r\r" + JournalInjection.LineChancesLabel + " 100% 25% 1%";

        Assert.False(JournalInjection.ShouldCaptureGameText(
            text,
            "Complete a line to receive a reward.",
            "Complete a line to receive a reward.",
            Marker));
    }

    [Fact]
    public void ShouldCaptureGameText_MarkerlessStalePluginOutputWithoutCapturedText_ReturnsFalse() {
        var text = "Complete a line to receive a reward.\r\r" + JournalInjection.LineChancesLabel + " 100% 25% 1%";

        Assert.False(JournalInjection.ShouldCaptureGameText(
            text,
            "Complete a line to receive a reward.",
            null,
            Marker));
    }
}
