using System;
using System.Text.Json.Serialization;

namespace AutoTyper.Models;

public class AutoTypeProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Shortcut { get; set; } = "F7";

    public string Text { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public int TypingDelayMs { get; set; } = 20;

    public bool UseJitter { get; set; } = false;

    public int MinDelayMs { get; set; } = 10;

    public int MaxDelayMs { get; set; } = 50;

    public TypingMode TypingMode { get; set; } = TypingMode.Simulated;

    public CapitalizationMode Capitalization { get; set; } = CapitalizationMode.Original;

    public int RepeatCount { get; set; } = 1;

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonIgnore]
    public string TextPreview
    {
        get
        {
            if (string.IsNullOrEmpty(Text))
                return string.Empty;

            var singleLine = Text.Replace("\r\n", " ").Replace("\n", " ").Trim();
            return singleLine.Length > 45 ? singleLine[..42] + "..." : singleLine;
        }
    }

    [JsonIgnore]
    public string FormattedDelay => UseJitter ? $"{MinDelayMs}-{MaxDelayMs}ms" : $"{TypingDelayMs}ms";

    public AutoTypeProfile Clone()
    {
        return new AutoTypeProfile
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(Name) ? "New Profile (Copy)" : $"{Name} (Copy)",
            Shortcut = Shortcut,
            Text = Text,
            Comment = Comment,
            TypingDelayMs = TypingDelayMs,
            UseJitter = UseJitter,
            MinDelayMs = MinDelayMs,
            MaxDelayMs = MaxDelayMs,
            TypingMode = TypingMode,
            Capitalization = Capitalization,
            RepeatCount = RepeatCount,
            IsEnabled = IsEnabled,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };
    }

    public void CopyFrom(AutoTypeProfile other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Name = other.Name;
        Shortcut = other.Shortcut;
        Text = other.Text;
        Comment = other.Comment;
        TypingDelayMs = other.TypingDelayMs;
        UseJitter = other.UseJitter;
        MinDelayMs = other.MinDelayMs;
        MaxDelayMs = other.MaxDelayMs;
        TypingMode = other.TypingMode;
        Capitalization = other.Capitalization;
        RepeatCount = other.RepeatCount;
        IsEnabled = other.IsEnabled;
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
