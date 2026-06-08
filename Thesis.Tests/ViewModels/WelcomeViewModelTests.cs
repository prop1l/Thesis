using System.Linq;
using Thesis.Models;
using Thesis.Models.Graph;
using Thesis.ViewModels;
using Xunit;

namespace Thesis.Tests.ViewModels;

public class WelcomeViewModelTests
{
    [Fact]
    public void AddGraph_AddsNewGraph()
    {
        using var folder = new TestFolder();
        var vm = new WelcomeViewModel(baseFolder: folder.PathValue);

        vm.NewGraphName = "TestGraph";

        Assert.True(vm.AddGraphCommand.CanExecute(null));

        vm.AddGraphCommand.Execute(null);

        Assert.Single(vm.GraphItems);
        Assert.Equal("TestGraph", vm.GraphItems[0].Name);
        Assert.Equal(string.Empty, vm.NewGraphName);
    }

    [Fact]
    public void AddGraph_DoesNotAddDuplicateName()
    {
        using var folder = new TestFolder();
        var vm = new WelcomeViewModel(baseFolder: folder.PathValue);

        vm.NewGraphName = "Graph1";
        vm.AddGraphCommand.Execute(null);

        vm.NewGraphName = "graph1";
        vm.AddGraphCommand.Execute(null);

        Assert.Single(vm.GraphItems);
        Assert.Equal("Graph1", vm.GraphItems[0].Name);
    }

    [Fact]
    public void AddGraph_DoesNotAddWhenNameIsEmpty()
    {
        using var folder = new TestFolder();
        var vm = new WelcomeViewModel(baseFolder: folder.PathValue);

        vm.NewGraphName = "   ";

        Assert.False(vm.AddGraphCommand.CanExecute(null));

        vm.AddGraphCommand.Execute(null);

        Assert.Empty(vm.GraphItems);
    }

    [Fact]
    public void RenameGraph_RenamesWhenUnique()
    {
        using var folder = new TestFolder();
        var vm = new WelcomeViewModel(baseFolder: folder.PathValue);

        vm.NewGraphName = "OldName";
        vm.AddGraphCommand.Execute(null);

        var item = vm.GraphItems.Single();
        var result = vm.RenameGraph(item, "NewName");

        Assert.True(result);
        Assert.Single(vm.GraphItems);
        Assert.Equal("NewName", vm.GraphItems[0].Name);
    }

    [Fact]
    public void RenameGraph_ReturnsFalseWhenDuplicateExists()
    {
        using var folder = new TestFolder();
        var vm = new WelcomeViewModel(baseFolder: folder.PathValue);

        vm.NewGraphName = "Graph1";
        vm.AddGraphCommand.Execute(null);

        vm.NewGraphName = "Graph2";
        vm.AddGraphCommand.Execute(null);

        var second = vm.GraphItems.Last();
        var result = vm.RenameGraph(second, "Graph1");

        Assert.False(result);
        Assert.Equal("Graph2", second.Name);
        Assert.Equal(2, vm.GraphItems.Count);
    }

    [Fact]
    public void DeleteGraph_RemovesGraphFromCollection()
    {
        using var folder = new TestFolder();
        var vm = new WelcomeViewModel(baseFolder: folder.PathValue);

        vm.NewGraphName = "Graph1";
        vm.AddGraphCommand.Execute(null);

        var item = vm.GraphItems.Single();

        vm.DeleteGraphCommand.Execute(item);

        Assert.Empty(vm.GraphItems);
    }

    [Fact]
    public void LoadGraphData_ReturnsEmptyGraphWhenFileDoesNotExist()
    {
        using var folder = new TestFolder();
        var vm = new WelcomeViewModel(baseFolder: folder.PathValue);

        var data = vm.LoadGraphData("MissingGraph");

        Assert.NotNull(data);
        Assert.Equal("MissingGraph", data!.Name);
        Assert.Empty(data.Nodes);
        Assert.Empty(data.Edges);
        Assert.NotNull(data.Style);
        Assert.Equal(GraphKind.UndirectedUnweighted, data.Kind);
    }

    [Fact]
    public void SaveGraphData_ThenLoadGraphData_RestoresGraph()
    {
        using var folder = new TestFolder();
        var vm = new WelcomeViewModel(baseFolder: folder.PathValue);

        var data = new GraphData
        {
            Name = "GraphA",
            Kind = GraphKind.DirectedWeighted,
            Nodes =
            {
                new Node { Label = "A", X = 10, Y = 20 },
                new Node { Label = "B", X = 30, Y = 40 }
            }
        };

        data.Edges.Add(new Edge
        {
            SourceId = data.Nodes[0].Id,
            TargetId = data.Nodes[1].Id,
            Label = "E1",
            Weight = 5
        });

        vm.SaveGraphData("GraphA", data);
        var loaded = vm.LoadGraphData("GraphA");

        Assert.NotNull(loaded);
        Assert.Equal("GraphA", loaded!.Name);
        Assert.Equal(GraphKind.DirectedWeighted, loaded.Kind);
        Assert.Equal(2, loaded.Nodes.Count);
        Assert.Single(loaded.Edges);
        Assert.Equal(5, loaded.Edges[0].Weight);
    }

    [Fact]
    public void AddGraph_CreatesPersistedGraphThatIsLoadedByNewInstance()
    {
        using var folder = new TestFolder();

        var vm1 = new WelcomeViewModel(baseFolder: folder.PathValue);
        vm1.NewGraphName = "PersistentGraph";
        vm1.AddGraphCommand.Execute(null);

        var vm2 = new WelcomeViewModel(baseFolder: folder.PathValue);

        Assert.Single(vm2.GraphItems);
        Assert.Equal("PersistentGraph", vm2.GraphItems[0].Name);

        var graph = vm2.LoadGraphData("PersistentGraph");
        Assert.NotNull(graph);
        Assert.Equal("PersistentGraph", graph!.Name);
    }
}