using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Thesis.Algorithms;
using Thesis.Models;
using Thesis.Models.Graph;
using Thesis.Services;
using Thesis.Tests.TestDoubles;
using Thesis.ViewModels.Dialogs;
using Thesis.ViewModels.Editor;
using Thesis.ViewModels.Settings;
using Thesis.ViewModels.Welcome;
using Xunit;

namespace Thesis.Tests.ViewModels;

// Поведение, добавленное вместе с новым интерфейсом
public class UiStateTests
{
    private sealed class FakeThemeService : IThemeService
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private ThemeMode _mode;
        private Color _accent = Colors.Blue;

        public ThemeMode Mode
        {
            get => _mode;
            set { _mode = value; PropertyChanged?.Invoke(this, new(nameof(Mode))); }
        }

        public Color AccentColor
        {
            get => _accent;
            set { _accent = value; PropertyChanged?.Invoke(this, new(nameof(AccentColor))); }
        }

        public bool IsDark => Mode == ThemeMode.Dark;
        public IReadOnlyList<Color> AccentPresets { get; } = [Colors.Blue, Colors.Red];
        public void ToggleLightDark() => Mode = IsDark ? ThemeMode.Light : ThemeMode.Dark;
    }

    [Fact]
    public void Welcome_NameError_ShownForDuplicateName()
    {
        using var folder = new TestFolder();
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue));
        vm.NewGraphName = "Graph";
        vm.AddGraphCommand.Execute(null);

        vm.NewGraphName = "graph ";

        Assert.True(vm.HasNameError);
        Assert.False(vm.AddGraphCommand.CanExecute(null));
    }

    [Fact]
    public void Welcome_CardShowsGraphStats()
    {
        using var folder = new TestFolder();
        var repository = new JsonGraphRepository(folder.PathValue);
        var data = repository.CreateEmptyGraph("G");
        data.Kind = GraphKind.DirectedWeighted;
        data.Nodes.Add(new Node());
        data.Nodes.Add(new Node());
        repository.SaveGraph("G", data);
        repository.SaveGraphList([new GraphItem { Name = "G" }]);

        var card = new WelcomeViewModel(repository).GraphItems.Single();

        Assert.Equal(2, card.NodesCount);
        Assert.Equal("2 вершины · 0 рёбер", card.StatsText);
        Assert.Equal("Ориентированный взвешенный", card.KindTitle);
    }

    [Fact]
    public async Task Welcome_DeleteGraph_RespectsConfirmation()
    {
        using var folder = new TestFolder();
        var windows = new FakeWindowService { ConfirmResult = false };
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue), windows);
        vm.NewGraphName = "Graph";
        vm.AddGraphCommand.Execute(null);

        await vm.DeleteGraphCommand.ExecuteAsync(vm.GraphItems[0]);
        Assert.Single(vm.GraphItems);

        windows.ConfirmResult = true;
        await vm.DeleteGraphCommand.ExecuteAsync(vm.GraphItems[0]);
        Assert.Empty(vm.GraphItems);
        Assert.Equal(2, windows.ConfirmShownCount);
    }

    [Fact]
    public void Welcome_ToggleTheme_SwitchesLightAndDark()
    {
        using var folder = new TestFolder();
        var theme = new FakeThemeService { Mode = ThemeMode.Light };
        var vm = new WelcomeViewModel(new JsonGraphRepository(folder.PathValue), themeService: theme);

        vm.ToggleThemeCommand.Execute(null);

        Assert.Equal(ThemeMode.Dark, theme.Mode);
        Assert.True(vm.IsDark);
    }

    [Fact]
    public void RenameDialog_RejectsTakenAndEmptyNames()
    {
        var vm = new RenameGraphDialogViewModel(["Busy"]) { GraphName = "busy" };
        Assert.False(vm.SaveCommand.CanExecute(null));
        Assert.True(vm.HasError);

        vm.GraphName = "   ";
        Assert.False(vm.CanSave);

        string? result = "unset";
        vm.CloseRequested += r => result = r;
        vm.GraphName = " Free ";
        vm.SaveCommand.Execute(null);

        Assert.Equal("Free", result);
    }

    [Fact]
    public void Settings_SelectionFollowsThemeService()
    {
        var theme = new FakeThemeService();
        var vm = new SettingWindowViewModel(theme);

        vm.SelectThemeModeCommand.Execute(vm.ThemeModes.Single(m => m.Mode == ThemeMode.Dark));
        vm.SelectAccentCommand.Execute(vm.Accents[1]);

        Assert.Equal(ThemeMode.Dark, theme.Mode);
        Assert.Equal(Colors.Red, theme.AccentColor);
        Assert.Equal([false, false, true], vm.ThemeModes.Select(m => m.IsSelected));
        Assert.Equal([false, true], vm.Accents.Select(a => a.IsSelected));
    }

    [Fact]
    public void Theme_AccentShadesStayReadableOnBothBackgrounds()
    {
        var light = ThemeService.ForLightTheme(Color.Parse("#A5B4FC")); // слишком светлый для белого фона
        var dark = ThemeService.ForDarkTheme(Color.Parse("#312E81"));   // слишком тёмный для тёмного фона

        Assert.True(light.ToHsl().L <= 0.5 + 1e-6);
        Assert.True(dark.ToHsl().L >= 0.68 - 1e-6);
    }

    [Fact]
    public void Settings_ArePersisted()
    {
        using var folder = new TestFolder();
        var store = new JsonSettingsStore(folder.PathValue);

        store.Save(new AppSettings { Theme = ThemeMode.Dark, AccentColor = "#FF059669" });
        var loaded = new JsonSettingsStore(folder.PathValue).Load();

        Assert.Equal(ThemeMode.Dark, loaded.Theme);
        Assert.Equal("#FF059669", loaded.AccentColor);
    }

    [Fact]
    public void Editor_StepCounterAndEmptyState()
    {
        using var folder = new TestFolder();
        var vm = new GraphEditorViewModel(new JsonGraphRepository(folder.PathValue), PathAlgorithmCatalog.CreateAll(), new ComplexityAnalyzer());
        vm.LoadGraph("G");
        Assert.True(vm.IsGraphEmpty);

        vm.AddNodeAtCenter(new Avalonia.Point(200, 200));
        vm.AddNodeAt(400, 200);
        Assert.False(vm.IsGraphEmpty);
        Assert.Equal(165, vm.Nodes[0].X); // центр в точке клика, вершина 70×70

        vm.AddEdge(vm.Nodes[0], vm.Nodes[1]);
        vm.SearchStartNode = vm.Nodes[0];
        vm.SearchEndNode = vm.Nodes[1];
        vm.RunSearchCommand.Execute(null);

        Assert.Equal($"Шаг 1 из {vm.SearchSteps.Count}", vm.StepCounterText);
        Assert.True(vm.StepProgress > 0);
    }
}
