using System.Collections.Generic;
using System.Linq;
using AutoTyper.Services.Engine;
using Xunit;

namespace AutoTyper.Tests;

public class TypingParserTests
{
    private readonly TypingParser _parser = new();

    [Fact]
    public void Parse_NullOrEmpty_ReturnsEmptyList()
    {
        Assert.Empty(_parser.Parse(null));
        Assert.Empty(_parser.Parse(string.Empty));
    }

    [Fact]
    public void Parse_PlainText_ReturnsSingleTextToken()
    {
        var tokens = _parser.Parse("Hello, World!");

        Assert.Single(tokens);
        Assert.Equal(TokenType.Text, tokens[0].Type);
        Assert.Equal("Hello, World!", tokens[0].Text);
    }

    [Theory]
    [InlineData("{ENTER}", 0x0D, "ENTER")]
    [InlineData("{enter}", 0x0D, "ENTER")]
    [InlineData("{RETURN}", 0x0D, "RETURN")]
    [InlineData("{TAB}", 0x09, "TAB")]
    [InlineData("{tab}", 0x09, "TAB")]
    [InlineData("{BACKSPACE}", 0x08, "BACKSPACE")]
    [InlineData("{backspace}", 0x08, "BACKSPACE")]
    [InlineData("{BACK}", 0x08, "BACK")]
    [InlineData("{SPACE}", 0x20, "SPACE")]
    [InlineData("{ESC}", 0x1B, "ESC")]
    [InlineData("{escape}", 0x1B, "ESCAPE")]
    [InlineData("{UP}", 0x26, "UP")]
    [InlineData("{DOWN}", 0x28, "DOWN")]
    [InlineData("{LEFT}", 0x25, "LEFT")]
    [InlineData("{RIGHT}", 0x27, "RIGHT")]
    [InlineData("{HOME}", 0x24, "HOME")]
    [InlineData("{END}", 0x23, "END")]
    [InlineData("{DELETE}", 0x2E, "DELETE")]
    [InlineData("{DEL}", 0x2E, "DEL")]
    [InlineData("{INSERT}", 0x2D, "INSERT")]
    [InlineData("{INS}", 0x2D, "INS")]
    [InlineData("{PAGEUP}", 0x21, "PAGEUP")]
    [InlineData("{PGUP}", 0x21, "PGUP")]
    [InlineData("{PAGEDOWN}", 0x22, "PAGEDOWN")]
    [InlineData("{PGDN}", 0x22, "PGDN")]
    [InlineData("{CAPSLOCK}", 0x14, "CAPSLOCK")]
    [InlineData("{NUMLOCK}", 0x90, "NUMLOCK")]
    [InlineData("{SCROLLLOCK}", 0x91, "SCROLLLOCK")]
    [InlineData("{PRINTSCREEN}", 0x2C, "PRINTSCREEN")]
    [InlineData("{PRTSC}", 0x2C, "PRTSC")]
    [InlineData("{PAUSE}", 0x13, "PAUSE")]
    [InlineData("{BREAK}", 0x13, "BREAK")]
    public void Parse_SpecialKeys_ParsedCorrectly(string input, ushort expectedVk, string expectedName)
    {
        var tokens = _parser.Parse(input);

        Assert.Single(tokens);
        Assert.Equal(TokenType.SpecialKey, tokens[0].Type);
        Assert.Equal(expectedVk, tokens[0].VirtualKeyCode);
        Assert.Equal(expectedName, tokens[0].Name);
    }

    [Theory]
    [InlineData("{F1}", 0x70)]
    [InlineData("{F2}", 0x71)]
    [InlineData("{F3}", 0x72)]
    [InlineData("{F4}", 0x73)]
    [InlineData("{F5}", 0x74)]
    [InlineData("{F6}", 0x75)]
    [InlineData("{F7}", 0x76)]
    [InlineData("{F8}", 0x77)]
    [InlineData("{F9}", 0x78)]
    [InlineData("{F10}", 0x79)]
    [InlineData("{F11}", 0x7A)]
    [InlineData("{F12}", 0x7B)]
    [InlineData("{f7}", 0x76)]
    public void Parse_FunctionKeys_ParsedCorrectly(string input, ushort expectedVk)
    {
        var tokens = _parser.Parse(input);

        Assert.Single(tokens);
        Assert.Equal(TokenType.SpecialKey, tokens[0].Type);
        Assert.Equal(expectedVk, tokens[0].VirtualKeyCode);
    }

    [Theory]
    [InlineData("{CTRL+C}", new ushort[] { 0x11 }, 0x43)]
    [InlineData("{ctrl+c}", new ushort[] { 0x11 }, 0x43)]
    [InlineData("{CTRL+V}", new ushort[] { 0x11 }, 0x56)]
    [InlineData("{CTRL+A}", new ushort[] { 0x11 }, 0x41)]
    [InlineData("{CTRL+Z}", new ushort[] { 0x11 }, 0x5A)]
    [InlineData("{CTRL+S}", new ushort[] { 0x11 }, 0x53)]
    [InlineData("{SHIFT+TAB}", new ushort[] { 0x10 }, 0x09)]
    [InlineData("{ALT+TAB}", new ushort[] { 0x12 }, 0x09)]
    [InlineData("{WIN+D}", new ushort[] { 0x5B }, 0x44)]
    [InlineData("{CTRL+SHIFT+S}", new ushort[] { 0x11, 0x10 }, 0x53)]
    [InlineData("{CTRL+ALT+DELETE}", new ushort[] { 0x11, 0x12 }, 0x2E)]
    public void Parse_KeyCombinations_ParsedCorrectly(string input, ushort[] expectedModifiers, ushort expectedTarget)
    {
        var tokens = _parser.Parse(input);

        Assert.Single(tokens);
        Assert.Equal(TokenType.KeyCombination, tokens[0].Type);
        Assert.Equal(expectedModifiers, tokens[0].Modifiers);
        Assert.Equal(expectedTarget, tokens[0].VirtualKeyCode);
    }

    [Fact]
    public void Parse_EscapedBraces_ProducesLiteralBraces()
    {
        var tokens = _parser.Parse("{{CTRL+C}}");

        Assert.Single(tokens);
        Assert.Equal(TokenType.Text, tokens[0].Type);
        Assert.Equal("{CTRL+C}", tokens[0].Text);
    }

    [Fact]
    public void Parse_EscapedDoubleBraces_ProducesSingleBraces()
    {
        var tokens = _parser.Parse("Literal {{opening}} and {{closing}} braces");

        Assert.Single(tokens);
        Assert.Equal(TokenType.Text, tokens[0].Type);
        Assert.Equal("Literal {opening} and {closing} braces", tokens[0].Text);
    }

    [Fact]
    public void Parse_UnknownTokenBraces_TreatedAsLiteralText()
    {
        var tokens = _parser.Parse("Hello {world} and {123}!");

        Assert.Single(tokens);
        Assert.Equal(TokenType.Text, tokens[0].Type);
        Assert.Equal("Hello {world} and {123}!", tokens[0].Text);
    }

    [Fact]
    public void Parse_MixedTextAndTokens_ParsesInExactSequence()
    {
        var tokens = _parser.Parse("Line 1{ENTER}Line 2{TAB}Indented{CTRL+S}");

        Assert.Equal(6, tokens.Count);
        Assert.Equal("Line 1", tokens[0].Text);
        Assert.Equal(TokenType.SpecialKey, tokens[1].Type);
        Assert.Equal(0x0D, tokens[1].VirtualKeyCode); // ENTER
        Assert.Equal("Line 2", tokens[2].Text);
        Assert.Equal(TokenType.SpecialKey, tokens[3].Type);
        Assert.Equal(0x09, tokens[3].VirtualKeyCode); // TAB
        Assert.Equal("Indented", tokens[4].Text);
        Assert.Equal(TokenType.KeyCombination, tokens[5].Type);
        Assert.Equal(0x53, tokens[5].VirtualKeyCode); // S
    }

    [Fact]
    public void Parse_UnmatchedBraces_TreatedAsLiteralText()
    {
        var tokens = _parser.Parse("Unmatched { brace and extra } brace");

        Assert.Single(tokens);
        Assert.Equal(TokenType.Text, tokens[0].Type);
        Assert.Equal("Unmatched { brace and extra } brace", tokens[0].Text);
    }

    [Theory]
    [InlineData("{WAIT:500}", 500)]
    [InlineData("{wait:1000}", 1000)]
    [InlineData("{WAIT:0}", 0)]
    [InlineData("{WaIt:2000}", 2000)]
    [InlineData("{WAIT:60000}", 60000)]
    public void Parse_WaitToken_ParsedCorrectly(string input, int expectedMs)
    {
        var tokens = _parser.Parse(input);

        Assert.Single(tokens);
        Assert.Equal(TokenType.Wait, tokens[0].Type);
        Assert.Equal(expectedMs, tokens[0].WaitMilliseconds);
        Assert.Equal(expectedMs, tokens[0].MinWaitMilliseconds);
        Assert.Equal(expectedMs, tokens[0].MaxWaitMilliseconds);
        Assert.False(tokens[0].IsWaitRange);
    }

    [Theory]
    [InlineData("{WAIT:20-400}", 20, 400)]
    [InlineData("{wait:100-1000}", 100, 1000)]
    [InlineData("{WAIT:0-60000}", 0, 60000)]
    [InlineData("{WAIT:400-20}", 20, 400)]       // Reversed range normalized
    [InlineData("{WAIT:1000-100}", 100, 1000)]   // Reversed range normalized
    public void Parse_WaitRangeToken_ParsedAndNormalized(string input, int expectedMin, int expectedMax)
    {
        var tokens = _parser.Parse(input);

        Assert.Single(tokens);
        Assert.Equal(TokenType.Wait, tokens[0].Type);
        Assert.Equal(expectedMin, tokens[0].MinWaitMilliseconds);
        Assert.Equal(expectedMax, tokens[0].MaxWaitMilliseconds);
        Assert.True(tokens[0].IsWaitRange);
    }

    [Theory]
    [InlineData("{WAIT:20-20}", 20)]
    [InlineData("{WAIT:200-200}", 200)]
    [InlineData("{WAIT:60000-60000}", 60000)]
    public void Parse_WaitEqualRange_ParsedCorrectly(string input, int expectedMs)
    {
        var tokens = _parser.Parse(input);

        Assert.Single(tokens);
        Assert.Equal(TokenType.Wait, tokens[0].Type);
        Assert.Equal(expectedMs, tokens[0].MinWaitMilliseconds);
        Assert.Equal(expectedMs, tokens[0].MaxWaitMilliseconds);
        Assert.False(tokens[0].IsWaitRange);
    }

    [Theory]
    [InlineData("{WAIT:-100}")]          // Negative value
    [InlineData("{WAIT:-20}")]           // Negative value
    [InlineData("{WAIT:500.5}")]         // Decimal
    [InlineData("{WAIT:abc}")]           // Non-numeric
    [InlineData("{WAIT:}")]              // Empty value
    [InlineData("{WAIT}")]               // Missing colon
    [InlineData("{WAIT:20-}")]           // Missing max
    [InlineData("{WAIT:-400}")]          // Missing min / negative
    [InlineData("{WAIT:-20-400}")]       // Negative min in range
    [InlineData("{WAIT:abc-400}")]       // Non-numeric min
    [InlineData("{WAIT:20-abc}")]        // Non-numeric max
    [InlineData("{WAIT:20-400-500}")]    // Multiple dashes
    [InlineData("{WAIT:20-60001}")]      // Upper bound exceeds 60000
    [InlineData("{WAIT:60001-60000}")]   // Lower bound exceeds 60000
    [InlineData("{WAIT:999999-1000}")]   // Out of range
    public void Parse_InvalidWaitSyntax_TreatedAsLiteralText(string input)
    {
        var tokens = _parser.Parse(input);

        Assert.Single(tokens);
        Assert.Equal(TokenType.Text, tokens[0].Type);
        Assert.Equal(input, tokens[0].Text);
    }

    [Fact]
    public void Parse_WaitExceedsMax_TreatedAsLiteralText()
    {
        var tokens = _parser.Parse("{WAIT:60001}");

        Assert.Single(tokens);
        Assert.Equal(TokenType.Text, tokens[0].Type);
        Assert.Equal("{WAIT:60001}", tokens[0].Text);

        var tokens2 = _parser.Parse("{WAIT:999999}");
        Assert.Single(tokens2);
        Assert.Equal(TokenType.Text, tokens2[0].Type);
    }

    [Fact]
    public void Parse_WaitMixedWithText_ParsesCorrectly()
    {
        var tokens = _parser.Parse("A{WAIT:100}B{WAIT:20-400}C");

        Assert.Equal(5, tokens.Count);
        Assert.Equal(TokenType.Text, tokens[0].Type);
        Assert.Equal("A", tokens[0].Text);

        Assert.Equal(TokenType.Wait, tokens[1].Type);
        Assert.Equal(100, tokens[1].WaitMilliseconds);
        Assert.False(tokens[1].IsWaitRange);

        Assert.Equal(TokenType.Text, tokens[2].Type);
        Assert.Equal("B", tokens[2].Text);

        Assert.Equal(TokenType.Wait, tokens[3].Type);
        Assert.Equal(20, tokens[3].MinWaitMilliseconds);
        Assert.Equal(400, tokens[3].MaxWaitMilliseconds);
        Assert.True(tokens[3].IsWaitRange);

        Assert.Equal(TokenType.Text, tokens[4].Type);
        Assert.Equal("C", tokens[4].Text);
    }
}
