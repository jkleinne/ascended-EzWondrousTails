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
        var text = "Base instruction." + Marker + "\r\rLine Chances: 50% 25% 1%";
        Assert.Equal("Base instruction.", JournalInjection.ExtractBaseText(text, Marker));
    }

    [Fact]
    public void ExtractBaseText_WrapBreaksAfterMarker_StillReturnsBase() {
        var text = "Base." + Marker + "Shuffle Advice (2 lines): Keep (+0.50pp\n2 line)";
        Assert.Equal("Base.", JournalInjection.ExtractBaseText(text, Marker));
    }

    [Fact]
    public void KeepFirstLine_SingleLine_ReturnsUnchanged() {
        Assert.Equal("Place stickers to fill the journal.",
            JournalInjection.KeepFirstLine("Place stickers to fill the journal."));
    }

    [Fact]
    public void KeepFirstLine_LineFeedSeparated_ReturnsFirstLine() {
        Assert.Equal("Instruction line.",
            JournalInjection.KeepFirstLine("Instruction line.\nOne or more lines of seals have been completed."));
    }

    [Fact]
    public void KeepFirstLine_CarriageReturnSeparated_ReturnsFirstLine() {
        Assert.Equal("Instruction line.",
            JournalInjection.KeepFirstLine("Instruction line.\rOne or more lines of seals have been completed."));
    }

    [Fact]
    public void KeepFirstLine_CarriageReturnLineFeed_ReturnsTextBeforeBreak() {
        Assert.Equal("Instruction line.",
            JournalInjection.KeepFirstLine("Instruction line.\r\nDeliver the journal to Khloe Aliapoh."));
    }

    [Fact]
    public void KeepFirstLine_EmptyString_ReturnsEmpty() {
        Assert.Equal(string.Empty, JournalInjection.KeepFirstLine(string.Empty));
    }

    [Fact]
    public void KeepFirstLine_AlreadyFirstLine_IsIdempotent() {
        var once = JournalInjection.KeepFirstLine("Instruction line.\rReminder.");
        Assert.Equal(once, JournalInjection.KeepFirstLine(once));
    }

    [Fact]
    public void KeepFirstLine_CompletionReminderInput_KeepsInstructionDropsReminder() {
        var text = "Complete the listed duties to add stickers.\rOne or more lines of seals have been completed. Deliver the journal to Khloe Aliapoh to receive your reward or continue adventuring.";
        Assert.Equal("Complete the listed duties to add stickers.", JournalInjection.KeepFirstLine(text));
    }
}
