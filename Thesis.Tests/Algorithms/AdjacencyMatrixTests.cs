using Thesis.Algorithms;
using Xunit;

namespace Thesis.Tests.Algorithms;

public class AdjacencyMatrixTests
{
    private static readonly GraphVertex[] Vertices = [new("a", "A"), new("b", "B")];
    private static readonly GraphArc[] Arcs = [new("ab", "a", "b", "AB", 5)];

    [Fact]
    public void Build_UndirectedWeighted_IsSymmetric()
    {
        var m = AdjacencyMatrix.Build(Vertices, Arcs, isDirected: false, isWeighted: true);

        Assert.Equal(5, m[0, 1]);
        Assert.Equal(5, m[1, 0]);
    }

    [Fact]
    public void Build_DirectedUnweighted_UsesOnesOneWay()
    {
        var m = AdjacencyMatrix.Build(Vertices, Arcs, isDirected: true, isWeighted: false);

        Assert.Equal(1, m[0, 1]);
        Assert.Equal(0, m[1, 0]);
    }

    [Fact]
    public void BuildText_EmptyGraph()
    {
        Assert.Equal("Граф пуст.", AdjacencyMatrix.BuildText([], [], false, false));
    }
}
