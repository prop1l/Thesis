using System.Collections.Generic;

namespace Thesis.Algorithms;

/// <summary>
/// Поиск в ширину: обходит граф «волнами» по очереди. Первая найденная дорога
/// до вершины содержит наименьшее число рёбер; веса не учитываются.
/// </summary>
public class BreadthFirstSearch : PathFindingAlgorithm
{
    public override string Name => "Поиск в ширину (BFS)";
    public override string Description =>
        "Обходит граф волнами с помощью очереди: сначала все соседи, потом соседи соседей. " +
        "Находит путь с наименьшим числом рёбер, веса не учитывает.";
    public override string Complexity => "O(V + E)";
    public override string DistanceCaption => "Число рёбер от начальной вершины";
    public override bool UsesWeights => false;
    public override bool GuaranteesShortestPath => true;

    protected override void Search(SearchRun run)
    {
        var queue = new Queue<string>();
        queue.Enqueue(run.StartId);
        run.Distances[run.StartId] = 0;
        run.Frontier.Add(run.StartId);
        run.Record("Инициализация", $"Вершина {run.Label(run.StartId)} помещается в очередь");

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            run.Frontier.Remove(current);
            run.Record("Извлечение из очереди",
                $"Из очереди взята вершина {run.Label(current)} (рёбер от начала: {SearchRun.Format(run.Distances[current])})",
                current);

            if (current == run.EndId)
            {
                run.Visited.Add(current);
                run.Record("Цель достигнута", $"Вершина {run.Label(current)} — конечная, поиск завершён", current);
                return;
            }

            foreach (var (arc, to) in run.Outgoing(current))
            {
                if (run.Distances[to] != decimal.MaxValue)
                {
                    run.Record("Просмотр ребра", $"Вершина {run.Label(to)} уже обнаружена — пропускаем", current, arc.Id);
                    continue;
                }

                run.Distances[to] = run.Distances[current] + 1;
                run.SetParent(to, current, arc);
                run.Frontier.Add(to);
                queue.Enqueue(to);
                run.Record("Добавление в очередь",
                    $"Вершина {run.Label(to)} обнаружена и поставлена в очередь (рёбер: {SearchRun.Format(run.Distances[to])})",
                    current, arc.Id);
            }

            run.Visited.Add(current);
            run.Record("Вершина обработана", $"Все соседи вершины {run.Label(current)} просмотрены", current);
        }
    }
}
