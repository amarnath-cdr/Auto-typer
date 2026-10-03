using AutoTyper.Models;

namespace AutoTyper.Services;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    void ApplyTheme(AppTheme theme);
}
