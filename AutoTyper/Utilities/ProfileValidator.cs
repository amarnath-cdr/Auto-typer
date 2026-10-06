using System.Collections.Generic;
using AutoTyper.Models;
using AutoTyper.Services.Hotkeys;

namespace AutoTyper.Utilities;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();

    public void AddError(string error) => Errors.Add(error);
}

public static class ProfileValidator
{
    public static ValidationResult Validate(AutoTypeProfile? profile)
    {
        var result = new ValidationResult();

        if (profile == null)
        {
            result.AddError("Profile cannot be null.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            result.AddError("Profile name is required.");
        }
        else if (profile.Name.Trim().Length > 100)
        {
            result.AddError("Profile name must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(profile.Shortcut))
        {
            result.AddError("Shortcut key is required.");
        }
        else if (!HotkeyModel.TryParse(profile.Shortcut, out _))
        {
            result.AddError($"Shortcut '{profile.Shortcut}' is not a valid key combination.");
        }

        if (profile.StartDelayMs < 0 || profile.StartDelayMs > 60000)
        {
            result.AddError("Start delay must be between 0ms and 60,000ms.");
        }

        if (profile.TypingDelayMs < 0 || profile.TypingDelayMs > 60000)
        {
            result.AddError("Typing delay must be between 0ms and 60,000ms.");
        }

        if (profile.UseJitter)
        {
            if (profile.MinDelayMs < 0 || profile.MinDelayMs > 60000)
            {
                result.AddError("Minimum jitter delay must be between 0ms and 60,000ms.");
            }

            if (profile.MaxDelayMs < 0 || profile.MaxDelayMs > 60000)
            {
                result.AddError("Maximum jitter delay must be between 0ms and 60,000ms.");
            }

            if (profile.MinDelayMs > profile.MaxDelayMs)
            {
                result.AddError("Minimum jitter delay cannot be greater than maximum jitter delay.");
            }
        }

        if (profile.RepeatCount < 1 || profile.RepeatCount > 1000)
        {
            result.AddError("Repeat count must be between 1 and 1,000.");
        }

        if (profile.Text == null)
        {
            result.AddError("Text cannot be null.");
        }

        return result;
    }
}
