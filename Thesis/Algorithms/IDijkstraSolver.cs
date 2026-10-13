using System.Collections.Generic;
using Thesis.Models.Algorithms;

namespace Thesis.Algorithms;

public interface IDijkstraSolver
{
    /// <summary>
    /// Выполняет алгоритм Дейкстры и возвращает все промежуточные шаги для пошаговой визуализации.
    /// Последний шаг всегда помечен IsFinished и содержит найденный путь (или пустой, если пути нет).
    /// </summary>
    IReadOnlyList<DijkstraStep> BuildSteps(
        IReadOnlyList<GraphVertex> vertices,
        IReadOnlyList<GraphArc> arcs,
        bool isDirected,
        string startId,
        string endId);
}
