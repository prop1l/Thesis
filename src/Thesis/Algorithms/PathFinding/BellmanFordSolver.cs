using System.Collections.Generic;
using System.Linq;

namespace Thesis.Algorithms;

/// <summary>
/// Алгоритм Беллмана — Форда: V − 1 раз «релаксирует» все рёбра. Медленнее Дейкстры,
/// но работает и с отрицательными весами и умеет обнаруживать отрицательные циклы.
/// </summary>
public class BellmanFordSolver : PathFindingAlgorithm
{
    public override string Name => "Алгоритм Беллмана — Форда";
    public override string Description =>
        "До V − 1 раз проходит по всем рёбрам и улучшает расстояния. Медленнее Дейкстры, " +
        "но допускает отрицательные веса и обнаруживает отрицательные циклы.";
    public override string Complexity => "O(V · E)";
    public override string DistanceCaption => "Расстояние от начальной вершины";
    public override bool UsesWeights => true;
    public override bool GuaranteesShortestPath => true;

    protected override void Search(SearchRun run)
    {
        run.Distances[run.StartId] = 0;
        run.Record("Инициализация",
            $"Расстояние до {run.Label(run.StartId)} равно 0, до остальных — ∞. Будет до {run.Vertices.Count - 1} проходов по рёбрам");

        // Неориентированное ребро — это два направленных
        var directed = run.Vertices.SelectMany(v => run.Outgoing(v.Id).Select(o => (From: v.Id, o.Arc, o.To))).ToList();

        for (var pass = 1; pass < run.Vertices.Count; pass++)
        {
            run.Frontier.Clear();
            run.Record($"Проход {pass}", $"Проход {pass} из {run.Vertices.Count - 1}: релаксация всех рёбер");
            var changed = false;

            foreach (var (from, arc, to) in directed)
            {
                if (run.Distances[from] == decimal.MaxValue)
                    continue;

                var candidate = run.Distances[from] + run.Length(arc);
                if (candidate >= run.Distances[to])
                    continue;

                run.Distances[to] = candidate;
                run.SetParent(to, from, arc);
                run.Frontier.Add(to);
                changed = true;
                run.Record("Релаксация ребра",
                    $"{run.Label(from)} → {run.Label(to)}: расстояние до {run.Label(to)} уменьшено до {SearchRun.Format(candidate)}",
                    from, arc.Id);
            }

            if (!changed)
            {
                run.Frontier.Clear();
                run.Record("Досрочное завершение", $"На проходе {pass} расстояния не изменились — дальше улучшать нечего");
                break;
            }
        }

        // Ещё одно улучшение после V − 1 проходов возможно только при отрицательном цикле
        foreach (var (from, arc, to) in directed)
        {
            if (run.Distances[from] != decimal.MaxValue && run.Distances[from] + run.Length(arc) < run.Distances[to])
            {
                run.Record("Отрицательный цикл", $"Ребро {run.Label(from)} → {run.Label(to)} всё ещё улучшает путь", from, arc.Id);
                run.FailureReason = "В графе есть цикл отрицательного веса — кратчайшего пути не существует";
                run.ExplicitPathNodes = new List<string>();
                return;
            }
        }

        foreach (var v in run.Vertices.Where(v => run.Distances[v.Id] != decimal.MaxValue))
            run.Visited.Add(v.Id);
    }
}
