using System.Linq;
using Avalonia;
using Thesis.Algorithms;
using Thesis.Models.Graph;
using Thesis.Services;
using Thesis.ViewModels;
using Xunit;

namespace Thesis.Tests.ViewModels;

// Раньше эта логика жила в code-behind окна и не тестировалась вовсе
public class GraphEditorInteractionTests
{
    // Вершины по умолчанию 70×70, поэтому центр вершины в (100, 100) — это (135, 135)
    private static readonly Point FirstCenter = new(135, 135);
    private static readonly Point SecondCenter = new(435, 135);
    private static readonly Point Empty = new(1000, 1000);

    private static (GraphEditorViewModel Vm, JsonGraphRepository Repository) CreateEditor(TestFolder folder, GraphKind kind)
    {
        var repository = new JsonGraphRepository(folder.PathValue);
        var vm = new GraphEditorViewModel(repository, new DijkstraSolver(), new ComplexityAnalyzer());

        vm.LoadGraph("Graph");
        vm.GraphKind = kind;
        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(400, 100);

        return (vm, repository);
    }

    [Fact]
    public void RightClickOnTwoNodes_CreatesEdge()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.DirectedUnweighted);

        vm.OnCanvasRightPressed(FirstCenter);
        Assert.True(vm.IsCreatingEdge);

        vm.OnCanvasRightPressed(SecondCenter);

        Assert.False(vm.IsCreatingEdge);
        var edge = Assert.Single(vm.Edges);
        Assert.Equal(vm.Nodes[0].Id, edge.SourceId);
        Assert.Equal(vm.Nodes[1].Id, edge.TargetId);
    }

    [Fact]
    public void RightClickOnEmptySpace_CancelsEdgeCreation()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.DirectedUnweighted);

        vm.OnCanvasRightPressed(FirstCenter);
        vm.OnCanvasRightPressed(Empty);
        vm.OnCanvasRightPressed(SecondCenter);

        Assert.Empty(vm.Edges);
        Assert.True(vm.IsCreatingEdge); // последний клик начал новое ребро
    }

    [Fact]
    public void PendingEdge_FollowsPointer()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.DirectedUnweighted);

        vm.OnCanvasRightPressed(FirstCenter);
        vm.OnCanvasPointerMoved(new Point(300, 300));

        Assert.Equal(FirstCenter, vm.PendingEdgeStart);
        Assert.Equal(new Point(300, 300), vm.PendingEdgeEnd);
    }

    [Fact]
    public void DragNode_MovesNodeAndSavesOnRelease()
    {
        using var folder = new TestFolder();
        var (vm, repository) = CreateEditor(folder, GraphKind.UndirectedUnweighted);

        vm.OnCanvasLeftPressed(FirstCenter);
        vm.OnCanvasPointerMoved(FirstCenter + new Point(50, 20));
        vm.OnCanvasPointerReleased();

        Assert.Equal(150, vm.Nodes[0].X);
        Assert.Equal(120, vm.Nodes[0].Y);

        var saved = repository.LoadGraph("Graph").Nodes.Single(n => n.Id == vm.Nodes[0].Id);
        Assert.Equal(150, saved.X);
    }

    [Fact]
    public void LeftClick_SelectsNodeEdgeAndClears()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);

        vm.OnCanvasLeftPressed(FirstCenter);
        Assert.Same(vm.Nodes[0], vm.SelectedNode);
        Assert.True(vm.Nodes[0].IsSelected);

        vm.OnCanvasLeftPressed(new Point(285, 135)); // середина ребра
        Assert.Same(vm.Edges[0], vm.SelectedEdge);
        Assert.True(vm.Edges[0].IsSelected);
        Assert.False(vm.Nodes[0].IsSelected);

        vm.OnCanvasLeftPressed(Empty);
        Assert.True(vm.IsNothingSelected);
        Assert.False(vm.Edges[0].IsSelected);
    }

    [Fact]
    public void EditingEdgeWeight_SyncsOppositeEdgeAndSaves()
    {
        using var folder = new TestFolder();
        var (vm, repository) = CreateEditor(folder, GraphKind.DirectedWeighted);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);
        vm.AddEdge(vm.Nodes[1], vm.Nodes[0]);

        // Так вес меняет NumericUpDown в панели свойств — напрямую через привязку
        vm.Edges[0].Weight = 7;

        Assert.Equal(7, vm.Edges[1].Weight);
        Assert.All(repository.LoadGraph("Graph").Edges, e => Assert.Equal(7, e.Weight));
    }

    [Fact]
    public void DijkstraStep_SetsExclusiveNodeVisualStates()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);
        vm.SelectedEdge = null;

        vm.DijkstraStartNode = vm.Nodes[0];
        vm.DijkstraEndNode = vm.Nodes[1];
        vm.RunDijkstraCommand.Execute(null);

        while (vm.CanGoToNextStep)
            vm.NextDijkstraStepCommand.Execute(null);

        // Финальный шаг: конечная вершина текущая, начальная — на пути
        Assert.True(vm.Nodes[1].ShowCurrent);
        Assert.True(vm.Nodes[0].ShowPath);
        Assert.False(vm.Nodes[0].ShowVisited);
        Assert.True(vm.Edges[0].IsPathEdge);
    }

    [Fact]
    public void DeleteSelectedNode_DetachesEdgesFromRemovedNode()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);
        var removedEdge = vm.Edges[0];

        vm.SelectedNode = vm.Nodes[0];
        var removedNode = vm.SelectedNode;
        vm.DeleteSelectedCommand.Execute(null);

        var raised = 0;
        removedEdge.PropertyChanged += (_, _) => raised++;
        removedNode!.X += 10;

        Assert.Equal(0, raised);
    }
}
