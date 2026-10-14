using System.Linq;
using Avalonia;
using Thesis.Algorithms;
using Thesis.Models.Graph;
using Thesis.Services;
using Thesis.Tests.TestDoubles;
using Thesis.ViewModels.Editor;
using Xunit;

namespace Thesis.Tests.ViewModels.Editor;

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
        var vm = new GraphEditorViewModel(repository, PathAlgorithmCatalog.CreateAll(), new ComplexityAnalyzer());

        vm.LoadGraph("Graph");
        vm.GraphKind = kind;
        vm.AddNodeAt(100, 100);
        vm.AddNodeAt(400, 100);

        return (vm, repository);
    }

    [Fact]
    public void DraggingNodeFarAway_IsNotLimitedAndGrowsContentBounds()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);

        vm.OnCanvasLeftPressed(FirstCenter);
        vm.OnCanvasPointerMoved(FirstCenter + new Point(3000, 2500));
        vm.OnCanvasPointerReleased();

        Assert.Equal(3100, vm.Nodes[0].X);
        // Вторая вершина осталась в (400, 100), поэтому область — от неё до перетащенной
        Assert.Equal(new Rect(400, 100, 2770, 2570), vm.ContentBounds);
    }

    [Fact]
    public void DraggingNodeBeyondTopLeft_GoesIntoNegativeCoordinates()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);

        vm.OnCanvasLeftPressed(FirstCenter);
        vm.OnCanvasPointerMoved(FirstCenter - new Point(500, 300));
        vm.OnCanvasPointerReleased();

        Assert.Equal(-400, vm.Nodes[0].X);
        Assert.Equal(-200, vm.Nodes[0].Y);
        Assert.Equal(new Point(-400, -200), vm.ContentBounds!.Value.TopLeft);
    }

    [Fact]
    public void EmptyGraph_HasNoContentBounds()
    {
        using var folder = new TestFolder();
        var vm = new GraphEditorViewModel(new JsonGraphRepository(folder.PathValue), PathAlgorithmCatalog.CreateAll(), new ComplexityAnalyzer());

        vm.LoadGraph("Graph");

        Assert.Null(vm.ContentBounds);
    }

    [Fact]
    public void ContentBounds_IsNotifiedWhenNodeMoves()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);
        var notified = false;
        vm.PropertyChanged += (_, e) => notified |= e.PropertyName == nameof(GraphEditorViewModel.ContentBounds);

        vm.Nodes[1].X = 900;

        Assert.True(notified);
        Assert.Equal(970, vm.ContentBounds!.Value.Right);
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

        vm.SearchStartNode = vm.Nodes[0];
        vm.SearchEndNode = vm.Nodes[1];
        vm.RunSearchCommand.Execute(null);

        while (vm.CanGoToNextStep)
            vm.NextStepCommand.Execute(null);

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

    [Fact]
    public void CompareAlgorithms_RunsEveryAlgorithmAndMarksBest()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedWeighted);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);
        vm.SearchStartNode = vm.Nodes[0];
        vm.SearchEndNode = vm.Nodes[1];

        vm.CompareAlgorithmsCommand.Execute(null);

        Assert.Equal(vm.Algorithms.Count, vm.ComparisonRows.Count);
        Assert.Equal(vm.Algorithms.Count, vm.StepsRanking.Count);
        Assert.NotEmpty(vm.ComparisonInsights);
        Assert.All(vm.ComparisonRows, r => Assert.True(r.Found));
        Assert.All(vm.ComparisonRows, r => Assert.True(r.IsBest));
        Assert.True(vm.HasComparison);
    }

    [Fact]
    public void ChangingAlgorithm_ClearsPreviousRun()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);
        vm.SearchStartNode = vm.Nodes[0];
        vm.SearchEndNode = vm.Nodes[1];
        vm.RunSearchCommand.Execute(null);
        Assert.True(vm.IsSearchMode);

        vm.SelectedAlgorithm = vm.Algorithms.Single(a => a is BreadthFirstSearch);

        Assert.False(vm.IsSearchMode);
        Assert.Empty(vm.SearchSteps);
        Assert.Contains("каждое ребро = 1", vm.AlgorithmPropertiesText);
    }

    [Fact]
    public void BreadthFirstStep_MarksQueuedNodesAsFrontier()
    {
        using var folder = new TestFolder();
        var (vm, _) = CreateEditor(folder, GraphKind.UndirectedUnweighted);
        vm.AddNodeAt(400, 400);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[2]);
        vm.SelectedAlgorithm = vm.Algorithms.Single(a => a is BreadthFirstSearch);
        vm.SearchStartNode = vm.Nodes[0];
        vm.SearchEndNode = vm.Nodes[2];
        vm.RunSearchCommand.Execute(null);

        while (vm.CurrentStep!.Title != "Вершина обработана")
            vm.NextStepCommand.Execute(null);

        Assert.True(vm.Nodes[1].ShowFrontier);
        Assert.True(vm.Nodes[2].ShowFrontier);
        Assert.True(vm.Nodes[0].IsVisited);   // отображается как «текущая» — у неё приоритет
    }
}
