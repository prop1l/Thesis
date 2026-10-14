using System.Collections.Generic;
using System.Linq;
using Thesis.Algorithms;
using Xunit;

namespace Thesis.Tests.Algorithms;

public class PathFindingAlgorithmsTests
{
    public static TheoryData<string> AllAlgorithms()
    {
        var data = new TheoryData<string>();
        foreach (var a in PathAlgorithmCatalog.CreateAll())
            data.Add(a.GetType().Name);
        return data;
    }

    private static IPathFindingAlgorithm Get(string typeName) =>
        PathAlgorithmCatalog.CreateAll().Single(a => a.GetType().Name == typeName);

    // Ромб: прямое ребро A–D тяжёлое (10), обход через B дешевле (1 + 1), через C — дороже (3 + 3)
    private static readonly GraphVertex[] Diamond =
    [
        new("a", "A", 0, 100), new("b", "B", 100, 0), new("c", "C", 100, 200), new("d", "D", 200, 100)
    ];

    private static readonly GraphArc[] DiamondArcs =
    [
        new("ab", "a", "b", "", 1), new("bd", "b", "d", "", 1),
        new("ac", "a", "c", "", 3), new("cd", "c", "d", "", 3),
        new("ad", "a", "d", "", 10)
    ];

    private static void AssertValidPath(SearchStep final, IReadOnlyList<GraphArc> arcs, string start, string end, bool directed)
    {
        Assert.Equal(start, final.FinalPathNodeIds.First());
        Assert.Equal(end, final.FinalPathNodeIds.Last());
        Assert.Equal(final.FinalPathNodeIds.Count - 1, final.FinalPathEdgeIds.Count);

        // Каждое ребро пути действительно соединяет соседние вершины пути
        for (var i = 0; i < final.FinalPathEdgeIds.Count; i++)
        {
            var arc = arcs.Single(a => a.Id == final.FinalPathEdgeIds[i]);
            var (from, to) = (final.FinalPathNodeIds[i], final.FinalPathNodeIds[i + 1]);
            var ok = (arc.SourceId == from && arc.TargetId == to) ||
                     (!directed && arc.SourceId == to && arc.TargetId == from);
            Assert.True(ok, $"ребро {arc.Id} не соединяет {from} → {to}");
        }
    }

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void EveryAlgorithm_FindsValidPathAndFinishes(string algorithm)
    {
        var steps = Get(algorithm).BuildSteps(Diamond, DiamondArcs, isDirected: false, "a", "d");
        var final = steps[^1];

        Assert.True(final.IsFinished);
        Assert.NotNull(final.PathCost);
        AssertValidPath(final, DiamondArcs, "a", "d", directed: false);
        Assert.Equal(Enumerable.Range(0, steps.Count), steps.Select(s => s.Index));
    }

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void EveryAlgorithm_ReportsMissingPath(string algorithm)
    {
        GraphVertex[] vertices = [new("a", "A"), new("b", "B"), new("c", "C")];
        GraphArc[] arcs = [new("ab", "a", "b", "", 1)];

        var final = Get(algorithm).BuildSteps(vertices, arcs, isDirected: true, "a", "c")[^1];

        Assert.True(final.IsFinished);
        Assert.Empty(final.FinalPathNodeIds);
        Assert.Null(final.PathCost);
        Assert.Equal("Путь не найден", final.Description);
    }

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void EveryAlgorithm_RespectsEdgeDirection(string algorithm)
    {
        GraphVertex[] vertices = [new("a", "A"), new("b", "B")];
        GraphArc[] arcs = [new("ba", "b", "a", "", 1)];

        var final = Get(algorithm).BuildSteps(vertices, arcs, isDirected: true, "a", "b")[^1];

        Assert.Empty(final.FinalPathNodeIds);
    }

    [Theory]
    [InlineData(nameof(DijkstraSolver))]
    [InlineData(nameof(AStarSearch))]
    [InlineData(nameof(BellmanFordSolver))]
    [InlineData(nameof(FloydWarshallSolver))]
    public void WeightedAlgorithms_FindMinimalTotalWeight(string algorithm)
    {
        var final = Get(algorithm).BuildSteps(Diamond, DiamondArcs, isDirected: false, "a", "d")[^1];

        Assert.Equal(2, final.PathCost);
        Assert.Equal(["a", "b", "d"], final.FinalPathNodeIds);
    }

    [Theory]
    [InlineData(nameof(BreadthFirstSearch))]
    [InlineData(nameof(BidirectionalSearch))]
    [InlineData(nameof(IterativeDeepeningSearch))]
    public void UnweightedAlgorithms_FindFewestEdges(string algorithm)
    {
        // По числу рёбер лучший путь — прямое ребро, хоть оно и самое тяжёлое
        var final = Get(algorithm).BuildSteps(Diamond, DiamondArcs, isDirected: false, "a", "d")[^1];

        Assert.Equal(["ad"], final.FinalPathEdgeIds);
        Assert.Equal(10, final.PathCost);
    }

    [Fact]
    public void GreedySearch_CanMissShortestPath_WhileAStarFindsIt()
    {
        // Вершина M лежит прямо на пути к цели, но ребро к ней очень дорогое; обход через N дешёвый
        GraphVertex[] vertices =
        [
            new("s", "S", 0, 0), new("m", "M", 100, 0), new("n", "N", 100, 150), new("g", "G", 200, 0)
        ];
        GraphArc[] arcs =
        [
            new("sm", "s", "m", "", 50), new("mg", "m", "g", "", 50),
            new("sn", "s", "n", "", 1), new("ng", "n", "g", "", 1)
        ];

        var greedy = new GreedyBestFirstSearch().BuildSteps(vertices, arcs, false, "s", "g")[^1];
        var astar = new AStarSearch().BuildSteps(vertices, arcs, false, "s", "g")[^1];

        Assert.Equal(100, greedy.PathCost);
        Assert.Equal(2, astar.PathCost);
    }

    [Fact]
    public void AStar_ProcessesFewerVerticesThanDijkstra_OnLongChain()
    {
        // Цепочка в обе стороны от старта: Дейкстре всё равно, куда идти, A* сразу идёт к цели
        var vertices = Enumerable.Range(-5, 11).Select(i => new GraphVertex($"v{i}", $"V{i}", i * 100, 0)).ToList();
        var arcs = Enumerable.Range(-5, 10).Select(i => new GraphArc($"e{i}", $"v{i}", $"v{i + 1}", "", 1)).ToList();

        var dijkstra = new DijkstraSolver().BuildSteps(vertices, arcs, false, "v0", "v5")[^1];
        var astar = new AStarSearch().BuildSteps(vertices, arcs, false, "v0", "v5")[^1];

        Assert.Equal(5, astar.PathCost);
        Assert.True(astar.VisitedNodeIds.Count < dijkstra.VisitedNodeIds.Count);
    }

    [Fact]
    public void BellmanFord_HandlesNegativeEdge()
    {
        GraphVertex[] vertices = [new("s", "S"), new("a", "A"), new("b", "B")];
        GraphArc[] arcs = [new("sa", "s", "a", "", 2), new("sb", "s", "b", "", 5), new("ba", "b", "a", "", -4)];

        var final = new BellmanFordSolver().BuildSteps(vertices, arcs, true, "s", "a")[^1];

        Assert.Equal(1, final.PathCost);
        Assert.Equal(["s", "b", "a"], final.FinalPathNodeIds);
    }

    [Fact]
    public void BellmanFord_DetectsNegativeCycle()
    {
        GraphVertex[] vertices = [new("s", "S"), new("a", "A"), new("b", "B")];
        GraphArc[] arcs = [new("sa", "s", "a", "", 1), new("ab", "a", "b", "", -3), new("ba", "b", "a", "", 1)];

        var final = new BellmanFordSolver().BuildSteps(vertices, arcs, true, "s", "b")[^1];

        Assert.Empty(final.FinalPathNodeIds);
        Assert.Contains("отрицательного веса", final.Description);
    }

    [Fact]
    public void BreadthFirstSearch_ShowsQueueAsFrontier()
    {
        var steps = new BreadthFirstSearch().BuildSteps(Diamond, DiamondArcs, false, "a", "d");

        // После обработки A в очереди оказываются все её соседи
        var afterA = steps.First(s => s.Title == "Вершина обработана" && s.CurrentNodeId == "a");
        Assert.Equal(new HashSet<string> { "b", "c", "d" }, afterA.FrontierNodeIds);
    }

    [Fact]
    public void DepthFirstSearch_GoesDeepBeforeWide()
    {
        // Звезда с длинным лучом: DFS доходит по первому лучу до конца, прежде чем взять второго соседа
        GraphVertex[] vertices = [new("s", "S"), new("a", "A"), new("a2", "A2"), new("b", "B")];
        GraphArc[] arcs = [new("sa", "s", "a", "", 1), new("aa2", "a", "a2", "", 1), new("sb", "s", "b", "", 1)];

        var order = new DepthFirstSearch().BuildSteps(vertices, arcs, false, "s", "b")
            .Where(s => s.Title == "Извлечение из стека").Select(s => s.CurrentNodeId).ToList();

        Assert.Equal(["s", "a", "a2", "b"], order);
    }

    [Fact]
    public void Catalog_HasNineDistinctAlgorithms()
    {
        var all = PathAlgorithmCatalog.CreateAll();

        Assert.Equal(9, all.Count);
        Assert.Equal(9, all.Select(a => a.Name).Distinct().Count());
        Assert.All(all, a => Assert.False(string.IsNullOrWhiteSpace(a.Description)));
    }
}
