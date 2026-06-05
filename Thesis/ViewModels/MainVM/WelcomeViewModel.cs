using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Thesis.Models.Graph;
using Thesis.Services;

namespace Thesis.ViewModels;

public partial class WelcomeViewModel : ObservableObject
{
    private readonly ThemeService? _themeService;
    private readonly string _filePath;

    public ObservableCollection<GraphItem> GraphItems { get; } = new();

    [ObservableProperty]
    private string newGraphName = string.Empty;

    public bool IsDarkTheme
    {
        get => _themeService?.IsDarkTheme ?? false;
        set
        {
            if (_themeService != null)
                _themeService.IsDarkTheme = value;
        }
    }

    public string AddNewGraphTitle => "Создать новый граф";
    public string AddNewGraphSubtitle => "Введите имя графа и добавьте его в систему.";

    public WelcomeViewModel(ThemeService? themeService = null)
    {
        _themeService = themeService;

        var appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GraphIS");

        Directory.CreateDirectory(appFolder);
        _filePath = Path.Combine(appFolder, "graphs.json");

        LoadGraphs();

        if (_themeService != null)
        {
            _themeService.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ThemeService.IsDarkTheme))
                    OnPropertyChanged(nameof(IsDarkTheme));
            };
        }
    }

    partial void OnNewGraphNameChanged(string value)
    {
        AddGraphCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddGraph))]
    private void AddGraph()
    {
        if (string.IsNullOrWhiteSpace(NewGraphName))
            return;

        var trimmedName = NewGraphName.Trim();

        if (GraphItems.Any(x => string.Equals(x.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
            return;

        GraphItems.Add(new GraphItem
        {
            Name = trimmedName
        });

        SaveGraphs();
        NewGraphName = string.Empty;
    }

    private bool CanAddGraph()
        => !string.IsNullOrWhiteSpace(NewGraphName);

    [RelayCommand]
    private void DeleteGraph(GraphItem? item)
    {
        if (item is null)
            return;

        if (GraphItems.Remove(item))
            SaveGraphs();
    }

    public bool RenameGraph(GraphItem item, string newName)
    {
        if (item is null || string.IsNullOrWhiteSpace(newName))
            return false;

        var trimmedName = newName.Trim();

        if (GraphItems.Any(x => !ReferenceEquals(x, item) &&
                                string.Equals(x.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
            return false;

        item.Name = trimmedName;
        SaveGraphs();
        return true;
    }

    private void LoadGraphs()
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            var json = File.ReadAllText(_filePath);
            var items = JsonSerializer.Deserialize<List<GraphItem>>(json);

            if (items is null)
                return;

            GraphItems.Clear();
            foreach (var item in items)
                GraphItems.Add(item);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки графов: {ex.Message}");
        }
    }

    private void SaveGraphs()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(GraphItems, options);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения графов: {ex.Message}");
        }
    }

    // Добавьте эти методы в класс WelcomeViewModel (ViewModels/WelcomeViewModel.cs)

    // Метод для загрузки данных графа
    public GraphData? LoadGraphData(string graphName)
    {
        var graphFilePath = GetGraphFilePath(graphName);
        if (!File.Exists(graphFilePath))
        {
            // Если файл не существует, создаем новый граф
            return new GraphData { Name = graphName };
        }

        try
        {
            var json = File.ReadAllText(graphFilePath);
            return JsonSerializer.Deserialize<GraphData>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки графа {graphName}: {ex.Message}");
            return null;
        }
    }

    public void SaveGraphData(string graphName, GraphData graphData)
    {
        if (graphData == null || string.IsNullOrWhiteSpace(graphName))
            return;

        var appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GraphIS");

        Directory.CreateDirectory(appFolder);

        var graphFilePath = Path.Combine(appFolder, $"{graphName}.json");

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(graphData, options);
            File.WriteAllText(graphFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения графа {graphName}: {ex.Message}");
        }
    }

    // Если у вас уже есть метод SaveGraphData с 1 параметром, удалите его или переименуйте

    // Вспомогательный метод для получения пути к файлу графа
    private string GetGraphFilePath(string graphName)
    {
        var appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GraphIS");

        Directory.CreateDirectory(appFolder);

        // Очищаем имя файла от недопустимых символов
        string safeFileName = string.Concat(graphName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(appFolder, $"{safeFileName}.json");
    }
}