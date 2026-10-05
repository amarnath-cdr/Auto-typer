using System;
using System.Text.Json;
using AutoTyper.Models;
using Xunit;

namespace AutoTyper.Tests;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Profile_Serialization_RoundTrip_PreservesAllProperties()
    {
        // Arrange
        var original = new AutoTypeProfile
        {
            Id = Guid.NewGuid(),
            Name = "Production Deploy Shortcut",
            Shortcut = "Ctrl+Shift+D",
            Text = "git push production main\r\n./deploy.sh",
            Comment = "Trigger deployment script",
            TypingDelayMs = 45,
            UseJitter = true,
            MinDelayMs = 20,
            MaxDelayMs = 80,
            TypingMode = TypingMode.Clipboard,
            Capitalization = CapitalizationMode.Uppercase,
            RepeatCount = 3,
            IsEnabled = false,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            ModifiedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AutoTypeProfile>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Shortcut, deserialized.Shortcut);
        Assert.Equal(original.Text, deserialized.Text);
        Assert.Equal(original.Comment, deserialized.Comment);
        Assert.Equal(original.TypingDelayMs, deserialized.TypingDelayMs);
        Assert.True(deserialized.UseJitter);
        Assert.Equal(original.MinDelayMs, deserialized.MinDelayMs);
        Assert.Equal(original.MaxDelayMs, deserialized.MaxDelayMs);
        Assert.Equal(original.TypingMode, deserialized.TypingMode);
        Assert.Equal(original.Capitalization, deserialized.Capitalization);
        Assert.Equal(original.RepeatCount, deserialized.RepeatCount);
        Assert.Equal(original.IsEnabled, deserialized.IsEnabled);
        Assert.Equal(original.CreatedAt, deserialized.CreatedAt);
        Assert.Equal(original.ModifiedAt, deserialized.ModifiedAt);
    }

    [Fact]
    public void Settings_Serialization_RoundTrip_PreservesAllProperties()
    {
        // Arrange
        var settings = new AppSettings
        {
            Theme = AppTheme.Dark,
            AutoTyperMasterEnabled = false,
            ConfirmOnDelete = false,
            DefaultDelayMs = 50,
            GlobalStopHotkey = "F10"
        };

        // Act
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(AppTheme.Dark, deserialized.Theme);
        Assert.False(deserialized.AutoTyperMasterEnabled);
        Assert.False(deserialized.ConfirmOnDelete);
        Assert.Equal(50, deserialized.DefaultDelayMs);
        Assert.Equal("F10", deserialized.GlobalStopHotkey);
    }

    [Theory]
    [InlineData("Short text", "Short text")]
    [InlineData("Line 1\r\nLine 2", "Line 1 Line 2")]
    [InlineData("This is a very long text that exceeds forty five characters threshold in the preview display", "This is a very long text that exceeds fort...")]
    public void TextPreview_CalculatesCorrectly(string input, string expected)
    {
        var profile = new AutoTypeProfile { Text = input };
        Assert.Equal(expected, profile.TextPreview);
    }

    [Fact]
    public void FormattedDelay_WithoutJitter_ReturnsDelayWithSuffix()
    {
        var profile = new AutoTypeProfile { TypingDelayMs = 25, UseJitter = false };
        Assert.Equal("25ms", profile.FormattedDelay);
    }

    [Fact]
    public void FormattedDelay_WithJitter_ReturnsRangeWithSuffix()
    {
        var profile = new AutoTypeProfile { UseJitter = true, MinDelayMs = 15, MaxDelayMs = 45 };
        Assert.Equal("15-45ms", profile.FormattedDelay);
    }
}
