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
}
