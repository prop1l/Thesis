using System.Linq;
using Thesis.Algorithms;
using Thesis.Models.Graph;
using Thesis.Services;
using Thesis.Tests.TestDoubles;
using Thesis.ViewModels.Editor;
using Xunit;

namespace Thesis.Tests.ViewModels.Editor;

public class AnalysisTests
{
    private static GraphEditorViewModel CreateTriangle(TestFolder folder, GraphKind kind)
    {
        var vm = new GraphEditorViewModel(new JsonGraphRepository(folder.PathValue), PathAlgorithmCatalog.CreateAll(), new ComplexityAnalyzer());
        vm.LoadGraph("G");
        vm.GraphKind = kind;
        vm.AddNodeAt(0, 0);
        vm.AddNodeAt(200, 0);
        vm.AddNodeAt(100, 200);
        vm.Nodes[0].Label = "A";
        vm.Nodes[1].Label = "B";
        vm.Nodes[2].Label = "C";
        return vm;
    }

    [Fact]
    public void Matrix_IsTableWithHeadersAndHighlightedEdges()
    {
        using var folder = new TestFolder();
        var vm = CreateTriangle(folder, GraphKind.DirectedWeighted);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);
        vm.Edges[0].Weight = 5;

        Assert.Equal(["A", "B", "C"], vm.MatrixHeaders);
        var rowA = vm.MatrixRows[0];
        Assert.Equal("A", rowA.Header);
        Assert.True(rowA.Cells[0].IsDiagonal);
        Assert.Equal("5", rowA.Cells[1].Text);
        Assert.True(rowA.Cells[1].HasEdge);
        Assert.False(vm.MatrixRows[1].Cells[0].HasEdge); // ориентированный: обратного ребра нет
    }

    [Fact]
    public void Matrix_UpdatesWhenVertexIsRenamed()
    {
        using var folder = new TestFolder();
        var vm = CreateTriangle(folder, GraphKind.UndirectedUnweighted);

        vm.Nodes[1].Label = "Москва";

        Assert.Equal("Москва", vm.MatrixHeaders[1]);
        Assert.Equal("Москва", vm.MatrixRows[1].Header);
    }

    [Fact]
    public void Matrix_UndirectedIsSymmetric()
    {
        using var folder = new TestFolder();
        var vm = CreateTriangle(folder, GraphKind.UndirectedUnweighted);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[2]);

        Assert.Equal("1", vm.MatrixRows[0].Cells[2].Text);
        Assert.Equal("1", vm.MatrixRows[2].Cells[0].Text);
        Assert.Contains("симметрична", vm.MatrixCaption);
    }

    [Fact]
    public void Statistics_DensityAndAverageDegree()
    {
        using var folder = new TestFolder();
        var vm = CreateTriangle(folder, GraphKind.UndirectedUnweighted);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);

        // 1 ребро из 3 возможных; степень 2·1/3
        Assert.Equal($"{1.0 / 3:P0}", vm.DensityText);
        Assert.Equal((2.0 / 3).ToString("0.#"), vm.AverageDegreeText);
    }

    [Fact]
    public void Comparison_BuildsRankingsAndInsights()
    {
        using var folder = new TestFolder();
        var vm = CreateTriangle(folder, GraphKind.UndirectedWeighted);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);
        vm.AddEdge(vm.Nodes[1], vm.Nodes[2]);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[2]);
        vm.Edges[2].Weight = 10; // прямое ребро A–C дороже обхода через B
        vm.SearchStartNode = vm.Nodes[0];
        vm.SearchEndNode = vm.Nodes[2];

        vm.CompareAlgorithmsCommand.Execute(null);

        Assert.Equal(vm.ComparisonRows.Select(r => r.StepCount).Order(), vm.StepsRanking.Select(r => r.StepCount));
        Assert.Equal(1.0, vm.StepsRanking[^1].StepsFraction);
        Assert.StartsWith("Минимальный вес пути — 2", vm.ComparisonInsights[0]);
        Assert.Contains(vm.ComparisonInsights, i => i.StartsWith("Путь тяжелее минимального"));
        Assert.Equal("Путь A → C", vm.ComparisonTitle);
    }

    [Fact]
    public void ShowComparison_OpensAnalysisTab()
    {
        using var folder = new TestFolder();
        var vm = CreateTriangle(folder, GraphKind.UndirectedUnweighted);
        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);
        vm.SearchStartNode = vm.Nodes[0];
        vm.SearchEndNode = vm.Nodes[1];

        vm.ShowComparisonCommand.Execute(null);

        Assert.Equal(GraphEditorViewModel.AnalysisTabIndex, vm.SelectedSideTabIndex);
        Assert.True(vm.HasComparison);
    }

    [Fact]
    public void Comparison_WhenUnreachable_ExplainsWhy()
    {
        using var folder = new TestFolder();
        var vm = CreateTriangle(folder, GraphKind.UndirectedUnweighted);
        vm.SearchStartNode = vm.Nodes[0];
        vm.SearchEndNode = vm.Nodes[1];

        vm.CompareAlgorithmsCommand.Execute(null);

        Assert.Contains("недостижима", vm.ComparisonInsights.Single());
    }

    [Fact]
    public void ComplexityChart_IsBuiltWithoutButtonPress()
    {
        using var folder = new TestFolder();
        var vm = CreateTriangle(folder, GraphKind.UndirectedUnweighted);

        Assert.NotEmpty(vm.ChartData);
    }
}
