using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Thesis.Models.Graph;

namespace Thesis.ViewModels;

/// <summary>Карточка графа на главном экране: имя и краткая сводка.</summary>
public partial class GraphCardViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatsText))]
    private int nodesCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatsText))]
    private int edgesCount;

    [ObservableProperty]
    private string kindTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModifiedText))]
    private DateTime lastModified;

    public GraphCardViewModel(GraphItem item)
    {
        Item = item;
    }

    public GraphItem Item { get; }

    public string Name
    {
        get => Item.Name;
        set => SetProperty(Item.Name, value, Item, (m, v) => m.Name = v);
    }

    public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name[..1].ToUpperInvariant();

    public string StatsText =>
        $"{NodesCount} {GraphKindText.Plural(NodesCount, "вершина", "вершины", "вершин")} · " +
        $"{EdgesCount} {GraphKindText.Plural(EdgesCount, "ребро", "ребра", "рёбер")}";

    public string ModifiedText => $"Изменён {LastModified:dd.MM.yyyy, HH:mm}";

    public void Update(GraphData data)
    {
        NodesCount = data.Nodes.Count;
        EdgesCount = data.Edges.Count;
        KindTitle = data.Kind.ToTitle();
        LastModified = data.LastModified;
        OnPropertyChanged(nameof(Initial));
    }
}
