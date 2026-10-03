using AutoTyper.Models;
using AutoTyper.Utilities;
using Xunit;

namespace AutoTyper.Tests;

public class ProfileValidationTests
{
    [Fact]
    public void Validate_ValidProfile_ReturnsSuccess()
    {
        var profile = new AutoTypeProfile
        {
            Name = "Valid Name",
            Shortcut = "F7",
            Text = "Sample text",
            TypingDelayMs = 20,
            RepeatCount = 1
        };

        var result = ProfileValidator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_NullProfile_ReturnsError()
    {
        var result = ProfileValidator.Validate(null);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("null"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceName_ReturnsError(string? name)
    {
        var profile = new AutoTypeProfile
        {
            Name = name!,
            Shortcut = "F7",
            Text = "Valid text"
        };

        var result = ProfileValidator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("name is required"));
    }

    [Fact]
    public void Validate_NameExceeding100Chars_ReturnsError()
    {
        var profile = new AutoTypeProfile
        {
            Name = new string('A', 101),
            Shortcut = "F7",
            Text = "Valid text"
        };

        var result = ProfileValidator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("100 characters"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyShortcut_ReturnsError(string? shortcut)
    {
        var profile = new AutoTypeProfile
        {
            Name = "Valid Name",
            Shortcut = shortcut!,
            Text = "Valid text"
        };

        var result = ProfileValidator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Shortcut"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60001)]
    public void Validate_InvalidTypingDelay_ReturnsError(int delay)
    {
        var profile = new AutoTypeProfile
        {
            Name = "Valid Name",
            Shortcut = "F7",
            Text = "Valid text",
            TypingDelayMs = delay
        };

        var result = ProfileValidator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Typing delay"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100001)]
    public void Validate_InvalidRepeatCount_ReturnsError(int count)
    {
        var profile = new AutoTypeProfile
        {
            Name = "Valid Name",
            Shortcut = "F7",
            Text = "Valid text",
            RepeatCount = count
        };

        var result = ProfileValidator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Repeat count"));
    }

    [Fact]
    public void Validate_NullText_ReturnsError()
    {
        var profile = new AutoTypeProfile
        {
            Name = "Valid Name",
            Shortcut = "F7",
            Text = null!
        };

        var result = ProfileValidator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Text cannot be null"));
    }
}
