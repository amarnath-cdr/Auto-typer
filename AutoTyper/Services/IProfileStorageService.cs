using System.Collections.Generic;
using System.Threading.Tasks;
using AutoTyper.Models;

namespace AutoTyper.Services;

public interface IProfileStorageService
{
    Task<List<AutoTypeProfile>> LoadProfilesAsync();
    Task SaveProfilesAsync(IEnumerable<AutoTypeProfile> profiles);
    List<AutoTypeProfile> LoadProfiles();
    void SaveProfiles(IEnumerable<AutoTypeProfile> profiles);
    List<AutoTypeProfile> GetDefaultProfiles();
}
