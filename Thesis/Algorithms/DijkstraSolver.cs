using System.Collections.Generic;
using System.Linq;
using Thesis.Models.Algorithms;

namespace Thesis.Algorithms;

public class DijkstraSolver : IDijkstraSolver
{
    public IReadOnlyList<DijkstraStep> BuildSteps(
        IReadOnlyList<GraphVertex> vertices,
        IReadOnlyList<GraphArc> arcs,
        bool isDirected,
        string startId,
        string endId)
    {
        var steps = new List<DijkstraStep>();
        var labels = vertices.ToDictionary(v => v.Id, v => v.Label);

        var dist = vertices.ToDictionary(v => v.Id, _ => decimal.MaxValue);
        var prev = vertices.ToDictionary(v => v.Id, _ => (string?)null);
        var visited = new HashSet<string>();

        dist[startId] = 0;

        AddStep(steps, "Инициализация",
            $"Начальная вершина: {Label(startId)}, конечная: {Label(endId)}",
            null, null, visited, dist, prev);

        while (true)
        {
            // Непосещённая вершина с минимальным известным расстоянием
            var current = vertices
                .Where(v => !visited.Contains(v.Id) && dist[v.Id] != decimal.MaxValue)
                .OrderBy(v => dist[v.Id])
                .FirstOrDefault();

            if (current is null)
                break;

            AddStep(steps, "Выбор вершины",
                $"Выбрана вершина {current.Label} с текущим расстоянием {dist[current.Id]}",
                current.Id, null, visited, dist, prev);

            visited.Add(current.Id);

            AddStep(steps, "Посещение вершины",
                $"Вершина {current.Label} помечена как обработанная",
                current.Id, null, visited, dist, prev);

            if (current.Id == endId)
                break;

            foreach (var arc in GetAdjacentArcs(arcs, current.Id, isDirected))
            {
                var neighborId = GetNeighborId(current.Id, arc, isDirected);
                if (neighborId is null || visited.Contains(neighborId))
                    continue;

                var candidate = dist[current.Id] + (arc.Weight ?? 1m);

                AddStep(steps, "Просмотр ребра",
                    $"Проверяем ребро {arc.Label}: {current.Label} -> {Label(neighborId)}",
                    current.Id, arc.Id, visited, dist, prev);

                if (candidate < dist[neighborId])
                {
                    dist[neighborId] = candidate;
                    prev[neighborId] = current.Id;

                    AddStep(steps, "Обновление расстояния",
                        $"Для вершины {Label(neighborId)} найден более короткий путь: {candidate}",
                        current.Id, arc.Id, visited, dist, prev);
                }
            }
        }

        var pathNodeIds = ReconstructPath(startId, endId, prev);
        var pathEdgeIds = GetPathEdgeIds(arcs, pathNodeIds, isDirected);

        steps.Add(new DijkstraStep
        {
            Index = steps.Count,
            Title = "Кратчайший путь",
            Description = pathNodeIds.Count == 0
                ? "Путь не найден"
                : $"Найден кратчайший путь длиной {dist[endId]}",
            CurrentNodeId = endId,
            VisitedNodeIds = new HashSet<string>(visited),
            Distances = new Dictionary<string, decimal>(dist),
            Previous = new Dictionary<string, string?>(prev),
            FinalPathNodeIds = pathNodeIds,
            FinalPathEdgeIds = pathEdgeIds,
            IsFinished = true
        });

        return steps;

        string Label(string id) => labels.GetValueOrDefault(id, id);
    }

    private static void AddStep(
        List<DijkstraStep> steps,
        string title,
        string description,
        string? currentNodeId,
        string? examinedEdgeId,
        HashSet<string> visited,
        Dictionary<string, decimal> distances,
        Dictionary<string, string?> previous)
    {
        // Каждый шаг хранит копии коллекций — это «снимок» состояния на момент шага
        steps.Add(new DijkstraStep
        {
            Index = steps.Count,
            Title = title,
            Description = description,
            CurrentNodeId = currentNodeId,
            ExaminedEdgeId = examinedEdgeId,
            VisitedNodeIds = new HashSet<string>(visited),
            Distances = new Dictionary<string, decimal>(distances),
            Previous = new Dictionary<string, string?>(previous)
        });
    }

    private static IEnumerable<GraphArc> GetAdjacentArcs(IReadOnlyList<GraphArc> arcs, string nodeId, bool isDirected)
    {
        return isDirected
            ? arcs.Where(a => a.SourceId == nodeId)
            : arcs.Where(a => a.SourceId == nodeId || a.TargetId == nodeId);
    }

    private static string? GetNeighborId(string currentId, GraphArc arc, bool isDirected)
    {
        if (arc.SourceId == currentId)
            return arc.TargetId;

        if (!isDirected && arc.TargetId == currentId)
            return arc.SourceId;

        return null;
    }

    private static List<string> ReconstructPath(string startId, string endId, Dictionary<string, string?> prev)
    {
        if (startId != endId && prev[endId] is null)
            return new List<string>();

        var path = new List<string>();
        string? current = endId;

        while (current is not null)
        {
            path.Add(current);

            if (current == startId)
                break;

            current = prev[current];
        }

        path.Reverse();

        return path.Count > 0 && path[0] == startId
            ? path
            : new List<string>();
    }

    private static List<string> GetPathEdgeIds(IReadOnlyList<GraphArc> arcs, List<string> pathNodeIds, bool isDirected)
    {
        var result = new List<string>();

        for (var i = 0; i < pathNodeIds.Count - 1; i++)
        {
            var from = pathNodeIds[i];
            var to = pathNodeIds[i + 1];

            var arc = isDirected
                ? arcs.FirstOrDefault(a => a.SourceId == from && a.TargetId == to)
                : arcs.FirstOrDefault(a =>
                    (a.SourceId == from && a.TargetId == to) ||
                    (a.SourceId == to && a.TargetId == from));

            if (arc is not null)
                result.Add(arc.Id);
        }

        return result;
    }
}
