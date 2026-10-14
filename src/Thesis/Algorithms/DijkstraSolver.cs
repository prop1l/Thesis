using System.Linq;

namespace Thesis.Algorithms;

/// <summary>
/// Алгоритм Дейкстры: на каждом шаге берёт ближайшую ещё не обработанную вершину
/// и пытается улучшить расстояния до её соседей. Работает с неотрицательными весами.
/// </summary>
public class DijkstraSolver : PathFindingAlgorithm
{
    public override string Name => "Алгоритм Дейкстры";
    public override string Description =>
        "Каждый раз обрабатывает ближайшую из необработанных вершин и улучшает расстояния до её соседей. " +
        "Находит кратчайший путь при неотрицательных весах.";
    public override string Complexity => "O((V + E) log V)";
    public override string DistanceCaption => "Расстояние от начальной вершины";
    public override bool UsesWeights => true;
    public override bool GuaranteesShortestPath => true;

    protected override void Search(SearchRun run)
    {
        run.Distances[run.StartId] = 0;
        run.Frontier.Add(run.StartId);
        run.Record("Инициализация",
            $"Расстояние до {run.Label(run.StartId)} равно 0, до остальных вершин — ∞");

        while (true)
        {
            // Непосещённая вершина с минимальным известным расстоянием
            var current = run.Vertices
                .Where(v => !run.Visited.Contains(v.Id) && run.Distances[v.Id] != decimal.MaxValue)
                .OrderBy(v => run.Distances[v.Id])
                .FirstOrDefault();

            if (current is null)
                break;

            run.Record("Выбор вершины",
                $"Выбрана вершина {current.Label} с наименьшим расстоянием {SearchRun.Format(run.Distances[current.Id])}",
                current.Id);

            run.Frontier.Remove(current.Id);
            run.Visited.Add(current.Id);
            run.Record("Посещение вершины", $"Вершина {current.Label} обработана, её расстояние окончательное", current.Id);

            if (current.Id == run.EndId)
                break;

            foreach (var (arc, to) in run.Outgoing(current.Id))
            {
                if (run.Visited.Contains(to))
                    continue;

                var candidate = run.Distances[current.Id] + run.Length(arc);
                run.Record("Просмотр ребра",
                    $"Ребро {current.Label} → {run.Label(to)}: {SearchRun.Format(run.Distances[current.Id])} + " +
                    $"{SearchRun.Format(run.Length(arc))} = {SearchRun.Format(candidate)}",
                    current.Id, arc.Id);

                if (candidate < run.Distances[to])
                {
                    run.Distances[to] = candidate;
                    run.SetParent(to, current.Id, arc);
                    run.Frontier.Add(to);
                    run.Record("Обновление расстояния",
                        $"Для вершины {run.Label(to)} найден более короткий путь: {SearchRun.Format(candidate)}",
                        current.Id, arc.Id);
                }
            }
        }
    }
}
