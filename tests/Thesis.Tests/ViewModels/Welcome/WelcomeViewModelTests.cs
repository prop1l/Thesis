using System.Linq;
using System.Threading.Tasks;
using Thesis.Services;
using Thesis.Tests.TestDoubles;
using Thesis.ViewModels.Welcome;
using Xunit;

namespace Thesis.Tests.ViewModels.Welcome;

public class WelcomeViewModelTests
{
    [Fact]
    public void AddGraph_AddsNewGraph()
    {
        using var folder = new TestFolder();
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue));

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
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue));

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
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue));

        vm.NewGraphName = "   ";

        Assert.False(vm.AddGraphCommand.CanExecute(null));

        vm.AddGraphCommand.Execute(null);

        Assert.Empty(vm.GraphItems);
    }

    [Fact]
    public void RenameGraph_RenamesWhenUnique()
    {
        using var folder = new TestFolder();
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue));

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
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue));

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
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue));

        vm.NewGraphName = "Graph1";
        vm.AddGraphCommand.Execute(null);

        var item = vm.GraphItems.Single();

        vm.DeleteGraphCommand.Execute(item);

        Assert.Empty(vm.GraphItems);
    }

    [Fact]
    public void AddGraph_CreatesPersistedGraphThatIsLoadedByNewInstance()
    {
        using var folder = new TestFolder();

        var vm1 = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue));
        vm1.NewGraphName = "PersistentGraph";
        vm1.AddGraphCommand.Execute(null);

        var vm2 = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue));

        Assert.Single(vm2.GraphItems);
        Assert.Equal("PersistentGraph", vm2.GraphItems[0].Name);

        var graph = new JsonGraphRepository(folder.PathValue).LoadGraph("PersistentGraph");
        Assert.Equal("PersistentGraph", graph.Name);
    }

    [Fact]
    public async Task RenameGraphCommand_UsesNameFromDialog()
    {
        using var folder = new TestFolder();
        var windows = new FakeWindowService { RenameDialogResult = "Renamed" };
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue), windows);

        vm.NewGraphName = "Original";
        vm.AddGraphCommand.Execute(null);

        await vm.RenameGraphCommand.ExecuteAsync(vm.GraphItems[0]);

        Assert.Equal("Renamed", vm.GraphItems.Single().Name);
    }

    [Fact]
    public async Task RenameGraphCommand_KeepsNameWhenDialogCancelled()
    {
        using var folder = new TestFolder();
        var windows = new FakeWindowService { RenameDialogResult = null };
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue), windows);

        vm.NewGraphName = "Original";
        vm.AddGraphCommand.Execute(null);

        await vm.RenameGraphCommand.ExecuteAsync(vm.GraphItems[0]);

        Assert.Equal("Original", vm.GraphItems.Single().Name);
    }

    [Fact]
    public async Task OpenGraphCommand_OpensEditorForGraph()
    {
        using var folder = new TestFolder();
        var windows = new FakeWindowService();
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue), windows);

        vm.NewGraphName = "Graph";
        vm.AddGraphCommand.Execute(null);

        await vm.OpenGraphCommand.ExecuteAsync(vm.GraphItems[0]);

        Assert.Equal(["Graph"], windows.OpenedEditors);
    }
}
