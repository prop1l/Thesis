using System.Linq;
using Thesis.Models;
using Thesis.Models.Graph;
using Thesis.ViewModels;
using Xunit;

namespace Thesis.Tests.ViewModels;

public class DijkstraTests
{
    [Fact]
    public void RunDijkstra_FindsShortestPath()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("DijkstraGraph");
        vm.GraphKind = GraphKind.DirectedWeighted;

        vm.AddNodeAt(0, 0);
        vm.AddNodeAt(100, 0);
        vm.AddNodeAt(200, 0);

        var a = vm.Nodes[0];
        var b = vm.Nodes[1];
        var c = vm.Nodes[2];

        a.Label = "A";
        b.Label = "B";
        c.Label = "C";

        vm.AddEdge(a, b);
        vm.SelectedEdge = vm.Edges.Last();
        vm.UpdateSelectedEdgeWeight(1);

        vm.AddEdge(b, c);
        vm.SelectedEdge = vm.Edges.Last();
        vm.UpdateSelectedEdgeWeight(2);

        vm.AddEdge(a, c);
        vm.SelectedEdge = vm.Edges.Last();
        vm.UpdateSelectedEdgeWeight(10);

        vm.DijkstraStartNode = a;
        vm.DijkstraEndNode = c;

        vm.RunDijkstraCommand.Execute(null);

        Assert.True(vm.IsDijkstraMode);
        Assert.NotEmpty(vm.DijkstraSteps);
        Assert.NotNull(vm.CurrentDijkstraStep);

        var finalStep = vm.DijkstraSteps.Last();

        Assert.True(finalStep.IsFinished);
        Assert.Equal(c.Id, finalStep.CurrentNodeId);
        Assert.Equal(3, finalStep.Distances[c.Id]);
        Assert.Equal(3, finalStep.FinalPathNodeIds.Count);
        Assert.Equal(a.Id, finalStep.FinalPathNodeIds[0]);
        Assert.Equal(b.Id, finalStep.FinalPathNodeIds[1]);
        Assert.Equal(c.Id, finalStep.FinalPathNodeIds[2]);
        Assert.Equal(2, finalStep.FinalPathEdgeIds.Count);
    }

    [Fact]
    public void RunDijkstra_WhenNoPath_FinishesWithEmptyFinalPath()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("NoPathGraph");
        vm.GraphKind = GraphKind.DirectedWeighted;

        vm.AddNodeAt(0, 0);
        vm.AddNodeAt(100, 0);
        vm.AddNodeAt(200, 0);

        var a = vm.Nodes[0];
        var b = vm.Nodes[1];
        var c = vm.Nodes[2];

        vm.AddEdge(a, b);
        vm.SelectedEdge = vm.Edges[0];
        vm.UpdateSelectedEdgeWeight(5);

        vm.DijkstraStartNode = a;
        vm.DijkstraEndNode = c;

        vm.RunDijkstraCommand.Execute(null);

        var finalStep = vm.DijkstraSteps.Last();

        Assert.True(finalStep.IsFinished);
        Assert.Empty(finalStep.FinalPathNodeIds);
        Assert.Empty(finalStep.FinalPathEdgeIds);
        Assert.Equal(decimal.MaxValue, finalStep.Distances[c.Id]);
    }

    [Fact]
    public void NextAndPreviousDijkstraStep_ChangesCurrentStepIndex()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("StepGraph");
        vm.GraphKind = GraphKind.DirectedWeighted;

        vm.AddNodeAt(0, 0);
        vm.AddNodeAt(100, 0);

        var a = vm.Nodes[0];
        var b = vm.Nodes[1];

        vm.AddEdge(a, b);
        vm.SelectedEdge = vm.Edges[0];
        vm.UpdateSelectedEdgeWeight(1);

        vm.DijkstraStartNode = a;
        vm.DijkstraEndNode = b;

        vm.RunDijkstraCommand.Execute(null);

        Assert.True(vm.CurrentDijkstraStepIndex >= 0);

        var firstIndex = vm.CurrentDijkstraStepIndex;

        if (vm.CanGoToNextStep)
        {
            vm.NextDijkstraStepCommand.Execute(null);
            Assert.True(vm.CurrentDijkstraStepIndex > firstIndex);

            vm.PreviousDijkstraStepCommand.Execute(null);
            Assert.Equal(firstIndex, vm.CurrentDijkstraStepIndex);
        }
    }

    [Fact]
    public void ClearDijkstra_ResetsState()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("ClearGraph");
        vm.GraphKind = GraphKind.DirectedWeighted;

        vm.AddNodeAt(0, 0);
        vm.AddNodeAt(100, 0);

        var a = vm.Nodes[0];
        var b = vm.Nodes[1];

        vm.AddEdge(a, b);
        vm.SelectedEdge = vm.Edges[0];
        vm.UpdateSelectedEdgeWeight(1);

        vm.DijkstraStartNode = a;
        vm.DijkstraEndNode = b;

        vm.RunDijkstraCommand.Execute(null);
        vm.ClearDijkstraCommand.Execute(null);

        Assert.False(vm.IsDijkstraMode);
        Assert.Empty(vm.DijkstraSteps);
        Assert.Equal(-1, vm.CurrentDijkstraStepIndex);
        Assert.All(vm.Nodes, n =>
        {
            Assert.False(n.IsVisited);
            Assert.False(n.IsHighlighted);
            Assert.Equal(string.Empty, n.DistanceLabel);
        });
        Assert.All(vm.Edges, e =>
        {
            Assert.False(e.IsExamined);
            Assert.False(e.IsHighlighted);
            Assert.False(e.IsPathEdge);
        });
    }
}