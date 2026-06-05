using System.Collections.Generic;

namespace Thesis.Models.Algorithms;

public class DijkstraStep
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? CurrentNodeId { get; set; }
    public string? ExaminedEdgeId { get; set; }

    public HashSet<string> VisitedNodeIds { get; set; } = new();
    public Dictionary<string, decimal> Distances { get; set; } = new();
    public Dictionary<string, string?> Previous { get; set; } = new();

    public List<string> FinalPathNodeIds { get; set; } = new();
    public List<string> FinalPathEdgeIds { get; set; } = new();

    public bool IsFinished { get; set; }
}