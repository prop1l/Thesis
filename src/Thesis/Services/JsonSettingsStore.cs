using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Thesis.Models;

namespace Thesis.Services;

/// <summary>Хранит настройки в settings.json рядом с графами.</summary>
public class JsonSettingsStore : ISettingsStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonSettingsStore(string folder)
    {
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_filePath), _jsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, _jsonOptions));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
        }
    }
}
