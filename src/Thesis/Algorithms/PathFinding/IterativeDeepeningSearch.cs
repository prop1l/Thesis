using System.Collections.Generic;

namespace Thesis.Algorithms;

/// <summary>
/// Поиск с итеративным углублением (IDDFS): поиск в глубину с ограничением глубины,
/// которое увеличивается на 1, пока цель не найдена. Сочетает экономию памяти DFS
/// с гарантией BFS найти путь с наименьшим числом рёбер.
/// </summary>
public class IterativeDeepeningSearch : PathFindingAlgorithm
{
    public override string Name => "Итеративное углубление (IDDFS)";
    public override string Description =>
        "Повторяет поиск в глубину с ограничением глубины 0, 1, 2, … пока не найдёт цель. " +
        "Расходует мало памяти и находит путь с наименьшим числом рёбер.";
    public override string Complexity => "O(V + E) на каждую глубину";
    public override string DistanceCaption => "Глубина на текущей итерации";
    public override bool UsesWeights => false;
    public override bool GuaranteesShortestPath => true;

    protected override void Search(SearchRun run)
    {
        run.Record("Инициализация", "Поиск начинается с ограничения глубины 0");

        for (var limit = 0; limit < run.Vertices.Count; limit++)
        {
            run.ResetMarks();
            run.Distances[run.StartId] = 0;
            run.Record($"Ограничение глубины {limit}",
                $"Новый поиск в глубину из {run.Label(run.StartId)}, глубже {limit} не спускаемся", run.StartId);

            var reachedNewNode = false;
            if (DepthLimited(run, run.StartId, limit, ref reachedNewNode))
                return;

            if (!reachedNewNode)
            {
                run.Record("Углубление не помогает", "Все достижимые вершины просмотрены — цель недостижима");
                return;
            }
        }
    }

    /// <returns>true, если цель найдена.</returns>
    private static bool DepthLimited(SearchRun run, string current, int remaining, ref bool reachedLimit)
    {
        run.Visited.Add(current);
        run.Frontier.Remove(current);
        run.Record("Переход в вершину",
            $"Вершина {run.Label(current)}, глубина {SearchRun.Format(run.Distances[current])}", current, run.PreviousArc[current]);

        if (current == run.EndId)
        {
            run.Record("Цель достигнута", $"Вершина {run.Label(current)} найдена на глубине {SearchRun.Format(run.Distances[current])}", current);
            return true;
        }

        if (remaining == 0)
        {
            // Упёрлись в ограничение, а рёбра дальше есть — значит, углубляться имеет смысл
            if (run.Outgoing(current).Count > 0)
                reachedLimit = true;
            return false;
        }

        foreach (var (arc, to) in run.Outgoing(current))
        {
            var depth = run.Distances[current] + 1;

            // Если вершина уже встречалась на этой итерации не глубже — повторный спуск ничего не даст
            if (run.Distances[to] <= depth)
                continue;

            run.Distances[to] = depth;
            run.SetParent(to, current, arc);
            run.Frontier.Add(to);

            if (DepthLimited(run, to, remaining - 1, ref reachedLimit))
                return true;
        }

        return false;
    }
}
