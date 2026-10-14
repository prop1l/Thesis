using System;
using System.Collections.ObjectModel;

namespace Thesis.Models.Graph;

public class GraphData
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<Node> Nodes { get; set; } = new();
    public ObservableCollection<Edge> Edges { get; set; } = new();
    public GraphStyle Style { get; set; } = new();
    public GraphKind Kind { get; set; } = GraphKind.UndirectedUnweighted;
    public DateTime LastModified { get; set; } = DateTime.Now;
}