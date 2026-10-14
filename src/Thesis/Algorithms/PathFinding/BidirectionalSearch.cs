using System.Collections.Generic;
using System.Linq;

namespace Thesis.Algorithms;

/// <summary>
/// Двунаправленный поиск: две волны поиска в ширину — от начала и от конца — идут навстречу.
/// Путь найден, когда волны встречаются. Обычно просматривает заметно меньше вершин, чем BFS.
/// </summary>
public class BidirectionalSearch : PathFindingAlgorithm
{
    public override string Name => "Двунаправленный поиск";
    public override string Description =>
        "Запускает поиск в ширину одновременно от начала и от конца; путь найден, когда волны встречаются. " +
        "Находит путь с наименьшим числом рёбер и обычно просматривает меньше вершин, чем BFS.";
    public override string Complexity => "O(V + E)";
    public override string DistanceCaption => "Число рёбер от ближайшего края поиска";
    public override bool UsesWeights => false;
    public override bool GuaranteesShortestPath => true;

    protected override void Search(SearchRun run)
    {
        var fromStart = new Dictionary<string, (string? Parent, GraphArc? Arc, int Depth)> { [run.StartId] = (null, null, 0) };
        var fromEnd = new Dictionary<string, (string? Parent, GraphArc? Arc, int Depth)> { [run.EndId] = (null, null, 0) };
        var forwardLevel = new List<string> { run.StartId };
        var backwardLevel = new List<string> { run.EndId };

        run.Distances[run.StartId] = 0;
        run.Distances[run.EndId] = 0;
        run.Frontier.Add(run.StartId);
        run.Frontier.Add(run.EndId);
        run.Record("Инициализация",
            $"Две волны: от {run.Label(run.StartId)} вперёд и от {run.Label(run.EndId)} назад");

        if (run.StartId == run.EndId)
        {
            Build(run, run.StartId, fromStart, fromEnd);
            return;
        }

        string? meet = null;
        var forward = true;

        while (forwardLevel.Count > 0 && backwardLevel.Count > 0 && meet is null)
        {
            var level = forward ? forwardLevel : backwardLevel;
            var own = forward ? fromStart : fromEnd;
            var other = forward ? fromEnd : fromStart;
            var next = new List<string>();
            var best = int.MaxValue;

            foreach (var current in level)
            {
                run.Frontier.Remove(current);
                run.Visited.Add(current);
                run.Record(forward ? "Шаг волны от начала" : "Шаг волны от конца",
                    $"Раскрываем вершину {run.Label(current)}", current);

                // Обратная волна идёт по рёбрам против их направления
                var neighbors = forward
                    ? run.Outgoing(current).Select(o => (o.Arc, Node: o.To))
                    : run.Incoming(current).Select(i => (i.Arc, Node: i.From));

                foreach (var (arc, to) in neighbors)
                {
                    if (own.ContainsKey(to))
                        continue;

                    own[to] = (current, arc, own[current].Depth + 1);
                    run.Distances[to] = own[to].Depth;
                    run.Frontier.Add(to);
                    next.Add(to);
                    run.Record("Новая вершина", $"Вершина {run.Label(to)} достигнута на расстоянии {own[to].Depth}", current, arc.Id);

                    if (other.TryGetValue(to, out var o) && own[to].Depth + o.Depth < best)
                    {
                        best = own[to].Depth + o.Depth;
                        meet = to;
                        run.Record("Волны встретились", $"Вершину {run.Label(to)} уже достигла встречная волна", to, arc.Id);
                    }
                }
            }

            if (forward)
                forwardLevel = next;
            else
                backwardLevel = next;
            forward = !forward;
        }

        if (meet is not null)
            Build(run, meet, fromStart, fromEnd);
    }

    private static void Build(SearchRun run, string meet,
        Dictionary<string, (string? Parent, GraphArc? Arc, int Depth)> fromStart,
        Dictionary<string, (string? Parent, GraphArc? Arc, int Depth)> fromEnd)
    {
        var nodes = new List<string>();
        var arcs = new List<string>();

        // Часть от начала до точки встречи (собираем с конца и переворачиваем)
        for (var n = meet; n is not null; n = fromStart[n].Parent)
        {
            nodes.Add(n);
            if (fromStart[n].Arc is { } a)
                arcs.Add(a.Id);
        }
        nodes.Reverse();
        arcs.Reverse();

        // Часть от точки встречи до конца
        for (var n = fromEnd[meet].Parent; n is not null; n = fromEnd[n].Parent)
            nodes.Add(n);
        for (var n = meet; fromEnd[n].Parent is { } p; n = p)
            arcs.Add(fromEnd[n].Arc!.Id);

        run.ExplicitPathNodes = nodes;
        run.ExplicitPathArcs = arcs;
    }
}
