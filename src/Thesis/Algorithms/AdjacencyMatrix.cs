using System;
using System.Collections.Generic;
using System.Linq;

namespace Thesis.Algorithms;

public static class AdjacencyMatrix
{
    public static decimal[,] Build(
        IReadOnlyList<GraphVertex> vertices,
        IReadOnlyList<GraphArc> arcs,
        bool isDirected,
        bool isWeighted)
    {
        var indexById = vertices
            .Select((vertex, index) => (vertex.Id, index))
            .ToDictionary(x => x.Id, x => x.index);

        var matrix = new decimal[vertices.Count, vertices.Count];

        foreach (var arc in arcs)
        {
            if (!indexById.TryGetValue(arc.SourceId, out var sourceIndex) ||
                !indexById.TryGetValue(arc.TargetId, out var targetIndex))
                continue;

            var value = isWeighted ? arc.Weight ?? 1m : 1m;

            matrix[sourceIndex, targetIndex] = value;

            if (!isDirected)
                matrix[targetIndex, sourceIndex] = value;
        }

        return matrix;
    }

    public static string BuildText(
        IReadOnlyList<GraphVertex> vertices,
        IReadOnlyList<GraphArc> arcs,
        bool isDirected,
        bool isWeighted)
    {
        if (vertices.Count == 0)
            return "Граф пуст.";

        return Format(vertices, Build(vertices, arcs, isDirected, isWeighted));
    }

    private static string Format(IReadOnlyList<GraphVertex> vertices, decimal[,] matrix)
    {
        var maxLabelLength = vertices.Max(v => string.IsNullOrWhiteSpace(v.Label) ? 1 : v.Label.Length);
        var rowHeaderWidth = Math.Max(4, maxLabelLength);
        var cellWidth = Math.Max(3, maxLabelLength);

        for (var i = 0; i < vertices.Count; i++)
            for (var j = 0; j < vertices.Count; j++)
                cellWidth = Math.Max(cellWidth, matrix[i, j].ToString("0.##").Length);

        var lines = new List<string>();

        var header = new List<string> { "".PadRight(rowHeaderWidth) };
        header.AddRange(vertices.Select(v => DisplayLabel(v).PadLeft(cellWidth)));
        lines.Add(string.Join(" ", header));

        for (var i = 0; i < vertices.Count; i++)
        {
            var row = new List<string> { DisplayLabel(vertices[i]).PadRight(rowHeaderWidth) };

            for (var j = 0; j < vertices.Count; j++)
                row.Add(matrix[i, j].ToString("0.##").PadLeft(cellWidth));

            lines.Add(string.Join(" ", row));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string DisplayLabel(GraphVertex vertex) =>
        string.IsNullOrWhiteSpace(vertex.Label) ? "-" : vertex.Label;
}
