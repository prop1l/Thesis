using System.Linq;
using Thesis.Algorithms;
using Xunit;

namespace Thesis.Tests.Algorithms;

// Алгоритм теперь чистая функция: тестируем без файлов, окон и ViewModel-ей
public class DijkstraSolverTests
{
    private readonly DijkstraSolver _solver = new();

    private static readonly GraphVertex[] Abc =
    [
        new("a", "A"),
        new("b", "B"),
        new("c", "C")
    ];

    [Fact]
    public void BuildSteps_PrefersCheaperIndirectPath()
    {
        GraphArc[] arcs =
        [
            new("ab", "a", "b", "AB", 1),
            new("bc", "b", "c", "BC", 2),
            new("ac", "a", "c", "AC", 10)
        ];

        var final = _solver.BuildSteps(Abc, arcs, isDirected: true, "a", "c").Last();

        Assert.True(final.IsFinished);
        Assert.Equal(3, final.Distances["c"]);
        Assert.Equal(["a", "b", "c"], final.FinalPathNodeIds);
        Assert.Equal(["ab", "bc"], final.FinalPathEdgeIds);
    }

    [Fact]
    public void BuildSteps_DirectedGraphIgnoresReverseEdges()
    {
        GraphArc[] arcs = [new("ba", "b", "a", "BA", 1)];
        GraphVertex[] vertices = [Abc[0], Abc[1]];

        var final = _solver.BuildSteps(vertices, arcs, isDirected: true, "a", "b").Last();

        Assert.Empty(final.FinalPathNodeIds);
        Assert.Equal(decimal.MaxValue, final.Distances["b"]);
    }

    [Fact]
    public void BuildSteps_UndirectedGraphUsesEdgesBothWays()
    {
        GraphArc[] arcs = [new("ba", "b", "a", "BA", 4)];
        GraphVertex[] vertices = [Abc[0], Abc[1]];

        var final = _solver.BuildSteps(vertices, arcs, isDirected: false, "a", "b").Last();

        Assert.Equal(4, final.Distances["b"]);
        Assert.Equal(["ba"], final.FinalPathEdgeIds);
    }

    [Fact]
    public void BuildSteps_UnweightedEdgesCountAsOne()
    {
        GraphArc[] arcs =
        [
            new("ab", "a", "b", "AB", null),
            new("bc", "b", "c", "BC", null)
        ];

        var final = _solver.BuildSteps(Abc, arcs, isDirected: false, "a", "c").Last();

        Assert.Equal(2, final.Distances["c"]);
    }

    [Fact]
    public void BuildSteps_NumbersStepsSequentially()
    {
        GraphArc[] arcs = [new("ab", "a", "b", "AB", 1)];

        var steps = _solver.BuildSteps(Abc, arcs, isDirected: false, "a", "b");

        Assert.Equal(Enumerable.Range(0, steps.Count), steps.Select(s => s.Index));
    }
}
