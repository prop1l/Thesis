using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Thesis.Models;
using Thesis.Models.Graph;
using Thesis.Services;

namespace Thesis.ViewModels;

public partial class WelcomeViewModel : ObservableObject
{
    private readonly ThemeService? _themeService;
    private readonly string _appFolder;
    private readonly string _graphsListFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

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

    public WelcomeViewModel(ThemeService? themeService = null, string? baseFolder = null)
    {
        _themeService = themeService;

        _appFolder = baseFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GraphIS");

        Directory.CreateDirectory(_appFolder);
        _graphsListFilePath = Path.Combine(_appFolder, "graphs.json");

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

        var item = new GraphItem
        {
            Name = trimmedName
        };

        GraphItems.Add(item);
        SaveGraphs();

        var graphData = CreateEmptyGraphData(trimmedName);
        SaveGraphData(trimmedName, graphData);

        NewGraphName = string.Empty;
    }

    private bool CanAddGraph()
        => !string.IsNullOrWhiteSpace(NewGraphName);

    [RelayCommand]
    private void DeleteGraph(GraphItem? item)
    {
        if (item is null)
            return;

        if (!GraphItems.Remove(item))
            return;

        SaveGraphs();

        try
        {
            var graphFilePath = GetGraphFilePath(item.Name);

            if (File.Exists(graphFilePath))
                File.Delete(graphFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка удаления файла графа {item.Name}: {ex.Message}");
        }
    }

    public bool RenameGraph(GraphItem item, string newName)
    {
        if (item is null || string.IsNullOrWhiteSpace(newName))
            return false;

        var trimmedName = newName.Trim();

        if (GraphItems.Any(x =>
                !ReferenceEquals(x, item) &&
                string.Equals(x.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
            return false;

        var oldName = item.Name;
        var oldPath = GetGraphFilePath(oldName);
        var newPath = GetGraphFilePath(trimmedName);

        try
        {
            if (!string.Equals(oldName, trimmedName, StringComparison.Ordinal))
            {
                if (File.Exists(oldPath))
                {
                    if (File.Exists(newPath))
                        File.Delete(newPath);

                    File.Move(oldPath, newPath);
                }
                else
                {
                    var newGraphData = CreateEmptyGraphData(trimmedName);
                    SaveGraphData(trimmedName, newGraphData);
                }
            }

            item.Name = trimmedName;
            SaveGraphs();

            var graphData = LoadGraphData(trimmedName) ?? CreateEmptyGraphData(trimmedName);
            graphData.Name = trimmedName;
            SaveGraphData(trimmedName, graphData);
            LoadGraphs();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка переименования графа {oldName} -> {trimmedName}: {ex.Message}");
            return false;
        }
    }

    private void LoadGraphs()
    {
        if (!File.Exists(_graphsListFilePath))
            return;

        try
        {
            var json = File.ReadAllText(_graphsListFilePath);
            var items = JsonSerializer.Deserialize<List<GraphItem>>(json, _jsonOptions);

            if (items is null)
                return;

            GraphItems.Clear();

            foreach (var item in items.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
                GraphItems.Add(item);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки списка графов: {ex.Message}");
        }
    }

    private void SaveGraphs()
    {
        try
        {
            var items = GraphItems
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => new GraphItem { Name = x.Name.Trim() })
                .ToList();

            var json = JsonSerializer.Serialize(items, _jsonOptions);
            File.WriteAllText(_graphsListFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения списка графов: {ex.Message}");
        }
    }

    public GraphData? LoadGraphData(string graphName)
    {
        if (string.IsNullOrWhiteSpace(graphName))
            return null;

        var normalizedName = graphName.Trim();
        var graphFilePath = GetGraphFilePath(normalizedName);

        if (!File.Exists(graphFilePath))
            return CreateEmptyGraphData(normalizedName);

        try
        {
            var json = File.ReadAllText(graphFilePath);
            var graphData = JsonSerializer.Deserialize<GraphData>(json, _jsonOptions);

            if (graphData is null)
                return CreateEmptyGraphData(normalizedName);

            graphData.Name = string.IsNullOrWhiteSpace(graphData.Name)
                ? normalizedName
                : graphData.Name;

            graphData.Nodes ??= new ObservableCollection<Node>();
            graphData.Edges ??= new ObservableCollection<Edge>();
            graphData.Style ??= new GraphStyle();

            return graphData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки графа {normalizedName}: {ex.Message}");
            return CreateEmptyGraphData(normalizedName);
        }
    }

    public void SaveGraphData(string graphName, GraphData graphData)
    {
        if (graphData is null || string.IsNullOrWhiteSpace(graphName))
            return;

        var normalizedName = graphName.Trim();
        var graphFilePath = GetGraphFilePath(normalizedName);

        try
        {
            graphData.Name = normalizedName;
            graphData.Nodes ??= new ObservableCollection<Node>();
            graphData.Edges ??= new ObservableCollection<Edge>();
            graphData.Style ??= new GraphStyle();
            graphData.LastModified = DateTime.Now;

            var json = JsonSerializer.Serialize(graphData, _jsonOptions);
            File.WriteAllText(graphFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения графа {normalizedName}: {ex.Message}");
        }
    }

    private GraphData CreateEmptyGraphData(string graphName)
    {
        return new GraphData
        {
            Name = graphName,
            Nodes = new ObservableCollection<Node>(),
            Edges = new ObservableCollection<Edge>(),
            Style = new GraphStyle(),
            Kind = GraphKind.UndirectedUnweighted,
            LastModified = DateTime.Now
        };
    }

    private string GetGraphFilePath(string graphName)
    {
        var safeFileName = SanitizeFileName(graphName);

        if (string.IsNullOrWhiteSpace(safeFileName))
            safeFileName = "graph";

        return Path.Combine(_appFolder, $"{safeFileName}.json");
    }

    private static string SanitizeFileName(string fileName)
    {
        var sanitized = fileName.Trim();

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
            sanitized = sanitized.Replace(invalidChar.ToString(), string.Empty);

        return sanitized.Trim();
    }
}