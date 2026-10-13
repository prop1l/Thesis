using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Thesis.Models;
using Thesis.Models.Graph;

namespace Thesis.Services;

/// <summary>
/// Хранит список графов в graphs.json, а каждый граф — в отдельном JSON-файле.
/// </summary>
public class JsonGraphRepository : IGraphRepository
{
    private readonly string _folder;
    private readonly string _graphsListFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public static string DefaultFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GraphIS");

    public JsonGraphRepository(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(_folder);
        _graphsListFilePath = Path.Combine(_folder, "graphs.json");
    }

    public IReadOnlyList<GraphItem> GetGraphList()
    {
        if (!File.Exists(_graphsListFilePath))
            return [];

        try
        {
            var json = File.ReadAllText(_graphsListFilePath);
            var items = JsonSerializer.Deserialize<List<GraphItem>>(json, _jsonOptions);

            return items?
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .ToList() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки списка графов: {ex.Message}");
            return [];
        }
    }

    public void SaveGraphList(IEnumerable<GraphItem> items)
    {
        try
        {
            var normalized = items
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => new GraphItem { Name = x.Name.Trim() })
                .ToList();

            var json = JsonSerializer.Serialize(normalized, _jsonOptions);
            File.WriteAllText(_graphsListFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения списка графов: {ex.Message}");
        }
    }

    public GraphData LoadGraph(string graphName)
    {
        var normalizedName = graphName.Trim();
        var graphFilePath = GetGraphFilePath(normalizedName);

        if (!File.Exists(graphFilePath))
            return CreateEmptyGraph(normalizedName);

        try
        {
            var json = File.ReadAllText(graphFilePath);
            var graphData = JsonSerializer.Deserialize<GraphData>(json, _jsonOptions);

            if (graphData is null)
                return CreateEmptyGraph(normalizedName);

            if (string.IsNullOrWhiteSpace(graphData.Name))
                graphData.Name = normalizedName;

            graphData.Nodes ??= new ObservableCollection<Node>();
            graphData.Edges ??= new ObservableCollection<Edge>();
            graphData.Style ??= new GraphStyle();

            return graphData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки графа {normalizedName}: {ex.Message}");
            return CreateEmptyGraph(normalizedName);
        }
    }

    public void SaveGraph(string graphName, GraphData graphData)
    {
        if (string.IsNullOrWhiteSpace(graphName))
            return;

        var normalizedName = graphName.Trim();

        try
        {
            graphData.Name = normalizedName;
            graphData.Nodes ??= new ObservableCollection<Node>();
            graphData.Edges ??= new ObservableCollection<Edge>();
            graphData.Style ??= new GraphStyle();
            graphData.LastModified = DateTime.Now;

            var json = JsonSerializer.Serialize(graphData, _jsonOptions);
            File.WriteAllText(GetGraphFilePath(normalizedName), json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения графа {normalizedName}: {ex.Message}");
        }
    }

    public void DeleteGraph(string graphName)
    {
        try
        {
            var graphFilePath = GetGraphFilePath(graphName);

            if (File.Exists(graphFilePath))
                File.Delete(graphFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка удаления файла графа {graphName}: {ex.Message}");
        }
    }

    public void RenameGraph(string oldName, string newName)
    {
        var oldPath = GetGraphFilePath(oldName);
        var newPath = GetGraphFilePath(newName);

        if (!File.Exists(oldPath))
        {
            SaveGraph(newName, CreateEmptyGraph(newName));
            return;
        }

        if (!string.Equals(oldPath, newPath, StringComparison.Ordinal))
            File.Move(oldPath, newPath, overwrite: true);

        // Имя хранится и внутри файла, поэтому перезаписываем его
        var graphData = LoadGraph(newName);
        SaveGraph(newName, graphData);
    }

    public GraphData CreateEmptyGraph(string graphName)
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

        return Path.Combine(_folder, $"{safeFileName}.json");
    }

    private static string SanitizeFileName(string fileName)
    {
        var sanitized = fileName.Trim();

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
            sanitized = sanitized.Replace(invalidChar.ToString(), string.Empty);

        return sanitized.Trim();
    }
}
