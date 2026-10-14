using Thesis.Models;

namespace Thesis.Services;

public interface ISettingsStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}
