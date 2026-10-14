using System.Collections.Generic;

namespace Thesis.Algorithms;

/// <summary>Все алгоритмы поиска пути в порядке показа в редакторе.</summary>
public static class PathAlgorithmCatalog
{
    public static IReadOnlyList<IPathFindingAlgorithm> CreateAll() =>
    [
        new DijkstraSolver(),
        new AStarSearch(),
        new BellmanFordSolver(),
        new FloydWarshallSolver(),
        new BreadthFirstSearch(),
        new BidirectionalSearch(),
        new IterativeDeepeningSearch(),
        new DepthFirstSearch(),
        new GreedyBestFirstSearch()
    ];
}
