using System.Linq;
using Thesis.Algorithms;
using Thesis.Models.Graph;
using Thesis.Services;
using Thesis.Tests.TestDoubles;
using Thesis.ViewModels.Editor;
using Xunit;

namespace Thesis.Tests.ViewModels.Editor;

public class DijkstraTests
{
    [Fact]
    public void RunSearch_FindsShortestPath()
    {
        using var folder = new TestFolder();
        var repository = new JsonGraphRepository(folder.PathValue);
        var vm = new GraphEditorViewModel(repository, PathAlgorithmCatalog.CreateAll(), new ComplexityAnalyzer());

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

        vm.SearchStartNode = a;
        vm.SearchEndNode = c;

        vm.RunSearchCommand.Execute(null);

        Assert.True(vm.IsSearchMode);
        Assert.NotEmpty(vm.SearchSteps);
        Assert.NotNull(vm.CurrentStep);

        var finalStep = vm.SearchSteps.Last();

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
    public void RunSearch_WhenNoPath_FinishesWithEmptyFinalPath()
    {
        using var folder = new TestFolder();
        var repository = new JsonGraphRepository(folder.PathValue);
        var vm = new GraphEditorViewModel(repository, PathAlgorithmCatalog.CreateAll(), new ComplexityAnalyzer());

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

        vm.SearchStartNode = a;
        vm.SearchEndNode = c;

        vm.RunSearchCommand.Execute(null);

        var finalStep = vm.SearchSteps.Last();

        Assert.True(finalStep.IsFinished);
        Assert.Empty(finalStep.FinalPathNodeIds);
        Assert.Empty(finalStep.FinalPathEdgeIds);
        Assert.Equal(decimal.MaxValue, finalStep.Distances[c.Id]);
    }

    [Fact]
    public void NextAndPreviousStep_ChangesCurrentStepIndex()
    {
        using var folder = new TestFolder();
        var repository = new JsonGraphRepository(folder.PathValue);
        var vm = new GraphEditorViewModel(repository, PathAlgorithmCatalog.CreateAll(), new ComplexityAnalyzer());

        vm.LoadGraph("StepGraph");
        vm.GraphKind = GraphKind.DirectedWeighted;

        vm.AddNodeAt(0, 0);
        vm.AddNodeAt(100, 0);

        var a = vm.Nodes[0];
        var b = vm.Nodes[1];

        vm.AddEdge(a, b);
        vm.SelectedEdge = vm.Edges[0];
        vm.UpdateSelectedEdgeWeight(1);

        vm.SearchStartNode = a;
        vm.SearchEndNode = b;

        vm.RunSearchCommand.Execute(null);

        Assert.True(vm.CurrentStepIndex >= 0);

        var firstIndex = vm.CurrentStepIndex;

        if (vm.CanGoToNextStep)
        {
            vm.NextStepCommand.Execute(null);
            Assert.True(vm.CurrentStepIndex > firstIndex);

            vm.PreviousStepCommand.Execute(null);
            Assert.Equal(firstIndex, vm.CurrentStepIndex);
        }
    }

    [Fact]
    public void ClearSearch_ResetsState()
    {
        using var folder = new TestFolder();
        var repository = new JsonGraphRepository(folder.PathValue);
        var vm = new GraphEditorViewModel(repository, PathAlgorithmCatalog.CreateAll(), new ComplexityAnalyzer());

        vm.LoadGraph("ClearGraph");
        vm.GraphKind = GraphKind.DirectedWeighted;

        vm.AddNodeAt(0, 0);
        vm.AddNodeAt(100, 0);

        var a = vm.Nodes[0];
        var b = vm.Nodes[1];

        vm.AddEdge(a, b);
        vm.SelectedEdge = vm.Edges[0];
        vm.UpdateSelectedEdgeWeight(1);

        vm.SearchStartNode = a;
        vm.SearchEndNode = b;

        vm.RunSearchCommand.Execute(null);
        vm.ClearSearchCommand.Execute(null);

        Assert.False(vm.IsSearchMode);
        Assert.Empty(vm.SearchSteps);
        Assert.Equal(-1, vm.CurrentStepIndex);
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