using System.Threading.Tasks;
using AutoTyper.Models;

namespace AutoTyper.Services;

public interface ISettingsStorageService
{
    Task<AppSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
}
