using System;
using System.Globalization;
using System.Linq;

namespace Thesis.Algorithms;

/// <summary>
/// Общая часть A* и жадного поиска: эвристика — расстояние по прямой до цели на холсте.
/// Чтобы оценка не превышала настоящую длину пути (иначе A* потеряет точность), она умножается
/// на наименьшее отношение «вес ребра / длина ребра на холсте» по всему графу.
/// </summary>
public abstract class HeuristicSearch : PathFindingAlgorithm
{
    /// <summary>Приоритет вершины в открытом множестве: для A* — g + h, для жадного поиска — только h.</summary>
    protected abstract decimal Priority(decimal g, decimal h);

    protected abstract string PriorityText(string label, decimal g, decimal h);

    protected override void Search(SearchRun run)
    {
        var scale = HeuristicScale(run);
        decimal H(string id) => Heuristic(run, id, scale);

        run.Distances[run.StartId] = 0;
        run.Frontier.Add(run.StartId);
        run.Record("Инициализация",
            $"Открытое множество: {{{run.Label(run.StartId)}}}. Оценка h — расстояние по прямой до {run.Label(run.EndId)}");

        while (run.Frontier.Count > 0)
        {
            // Вершина открытого множества с наименьшим приоритетом; при равенстве — ближе к цели
            var current = run.Frontier
                .OrderBy(id => Priority(run.Distances[id], H(id)))
                .ThenBy(H)
                .First();

            run.Record("Выбор вершины", PriorityText(run.Label(current), run.Distances[current], H(current)), current);

            run.Frontier.Remove(current);
            run.Visited.Add(current);

            if (current == run.EndId)
            {
                run.Record("Цель достигнута", $"Вершина {run.Label(current)} взята из открытого множества — поиск завершён", current);
                return;
            }

            foreach (var (arc, to) in run.Outgoing(current))
            {
                if (run.Visited.Contains(to))
                    continue;

                var g = run.Distances[current] + run.Length(arc);
                if (g >= run.Distances[to])
                {
                    run.Record("Просмотр ребра", $"Через {run.Label(current)} путь до {run.Label(to)} не короче — пропускаем", current, arc.Id);
                    continue;
                }

                run.Distances[to] = g;
                run.SetParent(to, current, arc);
                run.Frontier.Add(to);
                run.Record("Обновление вершины",
                    $"{run.Label(to)}: g = {SearchRun.Format(g)}, h = {Fmt(H(to))} — добавлена в открытое множество",
                    current, arc.Id);
            }
        }
    }

    private static decimal HeuristicScale(SearchRun run)
    {
        if (!run.UsesWeights)
            return 0.01m; // для невзвешенного графа оценка лишь подсказывает направление

        var ratios = run.Arcs
            .Select(a => (Arc: a, Length: Distance(run.Vertex(a.SourceId), run.Vertex(a.TargetId))))
            .Where(x => x.Length > 1e-6)
            .Select(x => (decimal)((double)run.Length(x.Arc) / x.Length))
            .ToList();

        return ratios.Count == 0 ? 0m : Math.Max(0m, ratios.Min());
    }

    private static decimal Heuristic(SearchRun run, string id, decimal scale) =>
        Math.Round((decimal)Distance(run.Vertex(id), run.Vertex(run.EndId)) * scale, 2);

    private static double Distance(GraphVertex a, GraphVertex b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    protected static string Fmt(decimal value) => value.ToString("0.##", CultureInfo.CurrentCulture);
}

/// <summary>
/// A*: как Дейкстра, но выбирает вершину с наименьшей суммой f = g + h, где g — пройденный путь,
/// h — оценка оставшегося. Благодаря оценке идёт в сторону цели и обрабатывает меньше вершин.
/// </summary>
public class AStarSearch : HeuristicSearch
{
    public override string Name => "Алгоритм A*";
    public override string Description =>
        "Выбирает вершину с наименьшим f = g + h: пройденный путь плюс оценка расстояния до цели по прямой. " +
        "Находит кратчайший путь и обычно обрабатывает меньше вершин, чем Дейкстра.";
    public override string Complexity => "O((V + E) log V)";
    public override string DistanceCaption => "g — длина пройденного пути";
    public override bool UsesWeights => true;
    public override bool GuaranteesShortestPath => true;

    protected override decimal Priority(decimal g, decimal h) => g + h;

    protected override string PriorityText(string label, decimal g, decimal h) =>
        $"Выбрана вершина {label}: f = g + h = {SearchRun.Format(g)} + {Fmt(h)} = {Fmt(g + h)}";
}

/// <summary>
/// Жадный поиск по первому наилучшему совпадению: всегда идёт к вершине, которая кажется
/// ближе всего к цели. Очень быстрый, но может найти не самый короткий путь.
/// </summary>
public class GreedyBestFirstSearch : HeuristicSearch
{
    public override string Name => "Жадный поиск (Best-First)";
    public override string Description =>
        "Всегда идёт к вершине, которая по прямой ближе всего к цели, не учитывая пройденный путь. " +
        "Обычно очень быстр, но путь может оказаться не кратчайшим.";
    public override string Complexity => "O((V + E) log V)";
    public override string DistanceCaption => "Длина пройденного пути";
    public override bool UsesWeights => true;
    public override bool GuaranteesShortestPath => false;

    protected override decimal Priority(decimal g, decimal h) => h;

    protected override string PriorityText(string label, decimal g, decimal h) =>
        $"Выбрана вершина {label}: по оценке до цели осталось h = {Fmt(h)}";
}
