using System.Collections.Generic;
using System.Linq;

namespace Thesis.Algorithms;

/// <summary>
/// Алгоритм Флойда — Уоршелла: находит кратчайшие пути сразу между всеми парами вершин,
/// по очереди разрешая проходить через каждую вершину как через промежуточную.
/// В редакторе показываются расстояния от выбранной начальной вершины.
/// </summary>
public class FloydWarshallSolver : PathFindingAlgorithm
{
    public override string Name => "Алгоритм Флойда — Уоршелла";
    public override string Description =>
        "Находит кратчайшие пути между всеми парами вершин: по очереди разрешает проходить через каждую вершину. " +
        "Под вершинами — расстояния от начальной.";
    public override string Complexity => "O(V³)";
    public override string DistanceCaption => "Расстояние от начальной вершины";
    public override bool UsesWeights => true;
    public override bool GuaranteesShortestPath => true;

    protected override void Search(SearchRun run)
    {
        var ids = run.Vertices.Select(v => v.Id).ToList();
        var dist = new Dictionary<(string, string), decimal>();
        var next = new Dictionary<(string, string), string?>();

        foreach (var i in ids)
        foreach (var j in ids)
        {
            dist[(i, j)] = i == j ? 0 : decimal.MaxValue;
            next[(i, j)] = i == j ? i : null;
        }

        foreach (var i in ids)
        foreach (var (arc, j) in run.Outgoing(i))
        {
            if (run.Length(arc) < dist[(i, j)])
            {
                dist[(i, j)] = run.Length(arc);
                next[(i, j)] = j;
            }
        }

        ShowRow(run, ids, dist);
        run.Record("Инициализация",
            $"Матрица расстояний заполнена весами рёбер. Строка {run.Label(run.StartId)} показана под вершинами");

        foreach (var k in ids)
        {
            run.Frontier.Clear();
            var improved = 0;

            foreach (var i in ids)
            foreach (var j in ids)
            {
                if (dist[(i, k)] == decimal.MaxValue || dist[(k, j)] == decimal.MaxValue)
                    continue;

                var through = dist[(i, k)] + dist[(k, j)];
                if (through < dist[(i, j)])
                {
                    dist[(i, j)] = through;
                    next[(i, j)] = next[(i, k)];
                    improved++;
                    if (i == run.StartId)
                        run.Frontier.Add(j);
                }
            }

            run.Visited.Add(k);
            ShowRow(run, ids, dist);
            run.Record($"Через вершину {run.Label(k)}",
                improved == 0
                    ? $"Путь через {run.Label(k)} не сокращает ни одного расстояния"
                    : $"Через {run.Label(k)} сокращено расстояний между парами: {improved}",
                k);
        }

        // Восстановление пути по матрице «следующих вершин»
        var nodes = new List<string>();
        var arcs = new List<string>();
        if (next[(run.StartId, run.EndId)] is not null)
        {
            var current = run.StartId;
            nodes.Add(current);
            while (current != run.EndId)
            {
                var step = next[(current, run.EndId)]!;
                arcs.Add(run.BestArc(current, step)!.Id);
                nodes.Add(step);
                current = step;
            }
        }

        run.ExplicitPathNodes = nodes;
        run.ExplicitPathArcs = arcs;
    }

    private static void ShowRow(SearchRun run, List<string> ids, Dictionary<(string, string), decimal> dist)
    {
        foreach (var j in ids)
            run.Distances[j] = dist[(run.StartId, j)];
    }
}
