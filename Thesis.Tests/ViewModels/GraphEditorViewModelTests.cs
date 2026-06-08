using System.Linq;
using Thesis.Models;
using Thesis.Models.Graph;
using Thesis.ViewModels;
using Xunit;

namespace Thesis.Tests.ViewModels;

public class GraphEditorViewModelTests
{
    [Fact]
    public void AddNode_IncreasesNodesCount()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("TestGraph");
        vm.AddNodeCommand.Execute(null);

        Assert.Equal(1, vm.NodesCount);
        Assert.Single(vm.Nodes);
        Assert.Equal("Вершина 1", vm.Nodes[0].Label);
    }

    [Fact]
    public void AddEdge_DoesNotCreateDuplicateInUndirectedGraph()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("TestGraph");
        vm.GraphKind = GraphKind.UndirectedUnweighted;

        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(200, 200);

        var first = vm.Nodes[0];
        var second = vm.Nodes[1];

        vm.AddEdge(first, second);
        vm.AddEdge(second, first);

        Assert.Single(vm.Edges);
    }

    [Fact]
    public void AddEdge_AllowsOppositeDirectionInDirectedGraph()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("DirectedGraph");
        vm.GraphKind = GraphKind.DirectedUnweighted;

        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(200, 200);

        var first = vm.Nodes[0];
        var second = vm.Nodes[1];

        vm.AddEdge(first, second);
        vm.AddEdge(second, first);

        Assert.Equal(2, vm.Edges.Count);
    }

    [Fact]
    public void DeleteSelected_RemovesNodeAndConnectedEdges()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("TestGraph");
        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(200, 200);

        var first = vm.Nodes[0];
        var second = vm.Nodes[1];

        vm.AddEdge(first, second);
        vm.SelectedNode = first;

        vm.DeleteSelectedCommand.Execute(null);

        Assert.Single(vm.Nodes);
        Assert.Equal(second.Id, vm.Nodes[0].Id);
        Assert.Empty(vm.Edges);
        Assert.Null(vm.SelectedNode);
    }

    [Fact]
    public void DeleteSelected_RemovesSelectedEdge()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("TestGraph");
        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(200, 200);

        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);
        vm.SelectedEdge = vm.Edges[0];

        vm.DeleteSelectedCommand.Execute(null);

        Assert.Empty(vm.Edges);
        Assert.Null(vm.SelectedEdge);
        Assert.Equal(2, vm.Nodes.Count);
    }

    [Fact]
    public void DeleteAll_RemovesAllNodesAndEdges()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("TestGraph");
        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(200, 200);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);

        vm.DeleteAllCommand.Execute(null);

        Assert.Empty(vm.Nodes);
        Assert.Empty(vm.Edges);
        Assert.Null(vm.SelectedNode);
        Assert.Null(vm.SelectedEdge);
    }

    [Fact]
    public void GraphKindChange_ClearsWeightsForUnweightedGraph()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("WeightedGraph");
        vm.GraphKind = GraphKind.UndirectedWeighted;

        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(200, 200);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);

        vm.SelectedEdge = vm.Edges[0];
        vm.UpdateSelectedEdgeWeight(7);

        vm.GraphKind = GraphKind.UndirectedUnweighted;

        Assert.Null(vm.Edges[0].Weight);
        Assert.False(vm.IsWeightedGraph);
    }

    [Fact]
    public void GraphKindChange_AddsDefaultWeightsForWeightedGraph()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("Graph");
        vm.GraphKind = GraphKind.UndirectedUnweighted;

        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(200, 200);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);

        Assert.Null(vm.Edges[0].Weight);

        vm.GraphKind = GraphKind.UndirectedWeighted;

        Assert.Equal(1, vm.Edges[0].Weight);
        Assert.True(vm.IsWeightedGraph);
    }

    [Fact]
    public void UpdateSelectedEdgeWeight_UpdatesWeightInWeightedGraph()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("Graph");
        vm.GraphKind = GraphKind.UndirectedWeighted;

        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(200, 200);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);

        vm.SelectedEdge = vm.Edges[0];
        vm.UpdateSelectedEdgeWeight(15);

        Assert.Equal(15, vm.Edges[0].Weight);
    }

    [Fact]
    public void ZoomLevel_IsClamped()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.ZoomLevel = 10;
        Assert.Equal(1.5, vm.ZoomLevel);

        vm.ZoomLevel = 0.1;
        Assert.Equal(0.5, vm.ZoomLevel);
    }

    [Fact]
    public void Selection_SelectingNodeClearsEdgeSelection()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("Graph");
        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(200, 200);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);

        vm.SelectedEdge = vm.Edges[0];
        vm.SelectedNode = vm.Nodes[0];

        Assert.NotNull(vm.SelectedNode);
        Assert.Null(vm.SelectedEdge);
        Assert.True(vm.IsNodeSelected);
        Assert.False(vm.IsEdgeSelected);
    }

    [Fact]
    public void Selection_SelectingEdgeClearsNodeSelection()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);
        var vm = new GraphEditorViewModel(welcome);

        vm.LoadGraph("Graph");
        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(200, 200);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);

        vm.SelectedNode = vm.Nodes[0];
        vm.SelectedEdge = vm.Edges[0];

        Assert.Null(vm.SelectedNode);
        Assert.NotNull(vm.SelectedEdge);
        Assert.False(vm.IsNodeSelected);
        Assert.True(vm.IsEdgeSelected);
    }

    [Fact]
    public void SaveGraph_PersistsNodesAndEdges()
    {
        using var folder = new TestFolder();
        var welcome = new WelcomeViewModel(baseFolder: folder.PathValue);

        var vm1 = new GraphEditorViewModel(welcome);
        vm1.LoadGraph("PersistentGraph");
        vm1.GraphKind = GraphKind.DirectedWeighted;
        vm1.AddNodeAt(10, 20);
        vm1.AddNodeAt(30, 40);
        vm1.AddEdge(vm1.Nodes[0], vm1.Nodes[1]);
        vm1.SelectedEdge = vm1.Edges[0];
        vm1.UpdateSelectedEdgeWeight(9);
        vm1.SaveGraph();

        var vm2 = new GraphEditorViewModel(welcome);
        vm2.LoadGraph("PersistentGraph");

        Assert.Equal(2, vm2.Nodes.Count);
        Assert.Single(vm2.Edges);
        Assert.Equal(GraphKind.DirectedWeighted, vm2.GraphKind);
        Assert.Equal(9, vm2.Edges[0].Weight);
    }
}