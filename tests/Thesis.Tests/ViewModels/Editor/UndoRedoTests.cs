using System.Linq;
using Avalonia;
using Thesis.Algorithms;
using Thesis.Models.Graph;
using Thesis.Services;
using Thesis.Tests.TestDoubles;
using Thesis.ViewModels.Editor;
using Xunit;

namespace Thesis.Tests.ViewModels.Editor;

public class UndoRedoTests
{
    // Вершины по умолчанию 70×70, поэтому центр вершины в (100, 100) — это (135, 135)
    private static readonly Point FirstCenter = new(135, 135);

    private static (GraphEditorViewModel Vm, JsonGraphRepository Repository) CreateEditor(TestFolder folder, GraphKind kind)
    {
        var repository = new JsonGraphRepository(folder.PathValue);
        var vm = new GraphEditorViewModel(repository, PathAlgorithmCatalog.CreateAll(), new ComplexityAnalyzer());

        vm.LoadGraph("Graph");
        vm.GraphKind = kind;
        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(400, 100);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);

        return (vm, repository);
    }

    [Fact]
    public void FreshlyLoadedGraph_HasNothingToUndo()
    {
        using var folder = new TestFolder();
        var vm = new GraphEditorViewModel(new JsonGraphRepository(folder.PathValue), PathAlgorithmCatalog.CreateAll(), new ComplexityAnalyzer());

        vm.LoadGraph("Graph");

        Assert.False(vm.CanUndo);
        Assert.False(vm.UndoCommand.CanExecute(null));
        Assert.False(vm.CanRedo);
    }

    [Fact]
    public void UndoAddNode_RemovesItAndRedoBringsItBack()
    {
        using var folder = new TestFolder();
        var (vm, repository) = CreateEditor(folder, GraphKind.UndirectedUnweighted);

        vm.AddNodeAt(700, 100);
        var addedId = vm.Nodes[^1].Id;

        vm.UndoCommand.Execute(null);
        Assert.Equal(2, vm.NodesCount);
        Assert.Equal(2, repository.LoadGraph("Graph").Nodes.Count);

        vm.RedoCommand.Execute(null);
        Assert.Equal(3, vm.NodesCount);
        Assert.Equal(addedId, vm.Nodes[^1].Id);
    }

    [Fact]
    public void UndoDeleteNode_RestoresNodeAndItsEdges()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);
        var nodeId = vm.Nodes[0].Id;
        var edgeId = vm.Edges[0].Id;

        vm.SelectedNode = vm.Nodes[0];
        vm.DeleteSelectedCommand.Execute(null);
        Assert.Equal(0, vm.EdgesCount);

        vm.UndoCommand.Execute(null);

        Assert.Contains(vm.Nodes, n => n.Id == nodeId);
        var edge = Assert.Single(vm.Edges);
        Assert.Equal(edgeId, edge.Id);
        // Ребро ссылается на новые объекты вершин, а не на удалённые
        Assert.Same(vm.Nodes.Single(n => n.Id == nodeId), edge.Source);
    }

    [Fact]
    public void Drag_IsUndoneInOneStep()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);

        vm.OnCanvasLeftPressed(FirstCenter);
        vm.OnCanvasPointerMoved(FirstCenter + new Point(10, 0));
        vm.OnCanvasPointerMoved(FirstCenter + new Point(50, 20));
        vm.OnCanvasPointerMoved(FirstCenter + new Point(200, 80));
        vm.OnCanvasPointerReleased();
        Assert.Equal(300, vm.Nodes[0].X);

        vm.UndoCommand.Execute(null);

        Assert.Equal(100, vm.Nodes[0].X);
        Assert.Equal(100, vm.Nodes[0].Y);
    }

    [Fact]
    public void TwoSeparateDrags_AreTwoSteps()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);

        vm.OnCanvasLeftPressed(FirstCenter);
        vm.OnCanvasPointerMoved(FirstCenter + new Point(100, 0));
        vm.OnCanvasPointerReleased();

        var moved = FirstCenter + new Point(100, 0);
        vm.OnCanvasLeftPressed(moved);
        vm.OnCanvasPointerMoved(moved + new Point(100, 0));
        vm.OnCanvasPointerReleased();

        vm.UndoCommand.Execute(null);
        Assert.Equal(200, vm.Nodes[0].X);

        vm.UndoCommand.Execute(null);
        Assert.Equal(100, vm.Nodes[0].X);
    }

    [Fact]
    public void ClickWithoutMoving_DoesNotCreateUndoStep()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);
        vm.UndoCommand.Execute(null); // отменили ребро, осталось 2 вершины
        vm.RedoCommand.Execute(null);

        vm.OnCanvasLeftPressed(FirstCenter);
        vm.OnCanvasPointerReleased();

        // Отмена по-прежнему убирает ребро, а не «пустой» клик
        vm.UndoCommand.Execute(null);
        Assert.Equal(0, vm.EdgesCount);
    }

    [Fact]
    public void TypingWeight_IsUndoneInOneStep()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedWeighted);
        var edge = vm.Edges[0];

        edge.Weight = 1;
        edge.Weight = 12;
        edge.Weight = 125;

        vm.UndoCommand.Execute(null);

        Assert.Equal(1, vm.Edges[0].Weight);
    }

    [Fact]
    public void UndoGraphKindChange_RestoresKindAndWeights()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedWeighted);
        vm.Edges[0].Weight = 7;

        vm.GraphKind = GraphKind.UndirectedUnweighted;
        Assert.Null(vm.Edges[0].Weight);

        vm.UndoCommand.Execute(null);

        Assert.Equal(GraphKind.UndirectedWeighted, vm.GraphKind);
        Assert.Equal(7, vm.Edges[0].Weight);
    }

    [Fact]
    public void NewChangeAfterUndo_ClearsRedo()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);

        vm.UndoCommand.Execute(null);
        Assert.True(vm.CanRedo);

        vm.AddNodeAt(700, 100);

        Assert.False(vm.CanRedo);
        Assert.False(vm.RedoCommand.CanExecute(null));
    }

    [Fact]
    public void UndoAll_ReturnsToLoadedGraph()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);

        while (vm.CanUndo)
            vm.UndoCommand.Execute(null);

        Assert.Equal(0, vm.NodesCount);
        Assert.Equal(GraphKind.UndirectedUnweighted, vm.GraphKind);
    }
}
