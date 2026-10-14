using System.Linq;
using Thesis.Models.Graph;
using Thesis.Services;
using Thesis.Tests.TestDoubles;
using Xunit;

namespace Thesis.Tests.Services;

public class JsonGraphRepositoryTests
{
    [Fact]
    public void LoadGraph_ReturnsEmptyGraphWhenFileDoesNotExist()
    {
        using var folder = new TestFolder();
        var repository = new JsonGraphRepository(folder.PathValue);

        var data = repository.LoadGraph("MissingGraph");

        Assert.Equal("MissingGraph", data.Name);
        Assert.Empty(data.Nodes);
        Assert.Empty(data.Edges);
        Assert.NotNull(data.Style);
        Assert.Equal(GraphKind.UndirectedUnweighted, data.Kind);
    }

    [Fact]
    public void SaveGraph_ThenLoadGraph_RestoresGraph()
    {
        using var folder = new TestFolder();
        var repository = new JsonGraphRepository(folder.PathValue);

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

        repository.SaveGraph("GraphA", data);
        var loaded = repository.LoadGraph("GraphA");

        Assert.Equal("GraphA", loaded.Name);
        Assert.Equal(GraphKind.DirectedWeighted, loaded.Kind);
        Assert.Equal(2, loaded.Nodes.Count);
        Assert.Single(loaded.Edges);
        Assert.Equal(5, loaded.Edges[0].Weight);
    }

    [Fact]
    public void SaveGraphList_ThenGetGraphList_SkipsEmptyNames()
    {
        using var folder = new TestFolder();
        var repository = new JsonGraphRepository(folder.PathValue);

        repository.SaveGraphList([new GraphItem { Name = " One " }, new GraphItem { Name = "  " }]);

        var names = repository.GetGraphList().Select(x => x.Name).ToList();

        Assert.Equal(["One"], names);
    }

    [Fact]
    public void RenameGraph_MovesDataAndUpdatesStoredName()
    {
        using var folder = new TestFolder();
        var repository = new JsonGraphRepository(folder.PathValue);

        var data = repository.CreateEmptyGraph("Old");
        data.Nodes.Add(new Node { Label = "A" });
        repository.SaveGraph("Old", data);

        repository.RenameGraph("Old", "New");

        var renamed = repository.LoadGraph("New");
        Assert.Equal("New", renamed.Name);
        Assert.Single(renamed.Nodes);
        Assert.Empty(repository.LoadGraph("Old").Nodes);
    }

    [Fact]
    public void DeleteGraph_RemovesGraphFile()
    {
        using var folder = new TestFolder();
        var repository = new JsonGraphRepository(folder.PathValue);

        var data = repository.CreateEmptyGraph("Graph");
        data.Nodes.Add(new Node());
        repository.SaveGraph("Graph", data);

        repository.DeleteGraph("Graph");

        Assert.Empty(repository.LoadGraph("Graph").Nodes);
    }
}
