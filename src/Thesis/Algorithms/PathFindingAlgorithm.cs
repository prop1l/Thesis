using System.Collections.Generic;

namespace Thesis.Algorithms;

/// <summary>
/// Общая основа алгоритмов: создаёт <see cref="SearchRun"/> с состоянием поиска,
/// запускает конкретный алгоритм и добавляет завершающий шаг с найденным путём.
/// </summary>
public abstract class PathFindingAlgorithm : IPathFindingAlgorithm
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Complexity { get; }
    public abstract string DistanceCaption { get; }
    public abstract bool UsesWeights { get; }
    public abstract bool GuaranteesShortestPath { get; }

    public IReadOnlyList<SearchStep> BuildSteps(
        IReadOnlyList<GraphVertex> vertices,
        IReadOnlyList<GraphArc> arcs,
        bool isDirected,
        string startId,
        string endId)
    {
        var run = new SearchRun(vertices, arcs, isDirected, startId, endId, UsesWeights);
        Search(run);
        run.Finish(GuaranteesShortestPath ? "Кратчайший путь" : "Найденный путь");
        return run.Steps;
    }

    /// <summary>Сам алгоритм: заполняет run.Distances / run.Previous и записывает шаги через run.Record.</summary>
    protected abstract void Search(SearchRun run);
}
