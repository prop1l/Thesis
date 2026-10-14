using System.Collections.Generic;
using System.Linq;

namespace Thesis.Algorithms;

/// <summary>
/// Поиск в глубину: идёт по одной ветви как можно дальше, возвращаясь назад по стеку.
/// Находит какой-нибудь путь, но не обязательно кратчайший.
/// </summary>
public class DepthFirstSearch : PathFindingAlgorithm
{
    public override string Name => "Поиск в глубину (DFS)";
    public override string Description =>
        "Уходит по одной ветви как можно дальше и возвращается назад по стеку. " +
        "Быстро находит какой-нибудь путь, но он не обязательно кратчайший.";
    public override string Complexity => "O(V + E)";
    public override string DistanceCaption => "Глубина в дереве обхода";
    public override bool UsesWeights => false;
    public override bool GuaranteesShortestPath => false;

    protected override void Search(SearchRun run)
    {
        // В стеке храним вершину вместе с тем, откуда в неё пришли
        var stack = new Stack<(string Node, string? From, GraphArc? Arc)>();
        stack.Push((run.StartId, null, null));
        run.Frontier.Add(run.StartId);
        run.Record("Инициализация", $"Вершина {run.Label(run.StartId)} помещается в стек");

        while (stack.Count > 0)
        {
            var (current, from, arc) = stack.Pop();
            if (run.Visited.Contains(current))
                continue;

            run.Distances[current] = from is null ? 0 : run.Distances[from] + 1;
            if (from is not null && arc is not null)
                run.SetParent(current, from, arc);

            run.Visited.Add(current);
            run.Frontier.Remove(current);
            run.Record("Извлечение из стека",
                $"Переходим в вершину {run.Label(current)} (глубина {SearchRun.Format(run.Distances[current])})",
                current, arc?.Id);

            if (current == run.EndId)
            {
                run.Record("Цель достигнута", $"Вершина {run.Label(current)} — конечная, поиск завершён", current);
                return;
            }

            // Соседей кладём в обратном порядке, чтобы первым из стека вышел первый сосед
            var pushed = 0;
            foreach (var (next, to) in run.Outgoing(current).Reverse())
            {
                if (run.Visited.Contains(to))
                    continue;

                stack.Push((to, current, next));
                run.Frontier.Add(to);
                pushed++;
            }

            run.Record("Соседи в стеке",
                pushed == 0
                    ? $"У вершины {run.Label(current)} нет новых соседей — возвращаемся назад"
                    : $"Непосещённые соседи вершины {run.Label(current)} помещены в стек",
                current);
        }
    }
}
