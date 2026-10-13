using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Thesis.Models.Graph;
using Thesis.Services;

namespace Thesis.ViewModels;

public partial class WelcomeViewModel : ObservableObject
{
    private readonly IGraphRepository _repository;
    private readonly IWindowService? _windowService;
    private readonly IThemeService? _themeService;

    public ObservableCollection<GraphItem> GraphItems { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddGraphCommand))]
    private string newGraphName = string.Empty;

    public bool IsDarkTheme
    {
        get => _themeService?.IsDarkTheme ?? false;
        set
        {
            if (_themeService != null)
                _themeService.IsDarkTheme = value;
        }
    }

    public string AddNewGraphTitle => "Создать новый граф";
    public string AddNewGraphSubtitle => "Введите имя графа и добавьте его в систему.";

    public WelcomeViewModel(
        IGraphRepository repository,
        IWindowService? windowService = null,
        IThemeService? themeService = null)
    {
        _repository = repository;
        _windowService = windowService;
        _themeService = themeService;

        LoadGraphs();

        if (_themeService != null)
        {
            _themeService.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IThemeService.IsDarkTheme))
                    OnPropertyChanged(nameof(IsDarkTheme));
            };
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddGraph))]
    private void AddGraph()
    {
        // Execute не проверяет CanExecute сам, поэтому защита нужна и здесь
        if (!CanAddGraph())
            return;

        var trimmedName = NewGraphName.Trim();

        if (GraphItems.Any(x => string.Equals(x.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
            return;

        GraphItems.Add(new GraphItem { Name = trimmedName });
        _repository.SaveGraphList(GraphItems);
        _repository.SaveGraph(trimmedName, _repository.CreateEmptyGraph(trimmedName));

        NewGraphName = string.Empty;
    }

    private bool CanAddGraph()
        => !string.IsNullOrWhiteSpace(NewGraphName);

    [RelayCommand]
    private void DeleteGraph(GraphItem? item)
    {
        if (item is null || !GraphItems.Remove(item))
            return;

        _repository.SaveGraphList(GraphItems);
        _repository.DeleteGraph(item.Name);
    }

    [RelayCommand]
    private async Task OpenGraphAsync(GraphItem? item)
    {
        if (item is not null && _windowService is not null)
            await _windowService.ShowGraphEditorAsync(item.Name);
    }

    [RelayCommand]
    private async Task RenameGraphAsync(GraphItem? item)
    {
        if (item is null || _windowService is null)
            return;

        var newName = await _windowService.ShowRenameGraphDialogAsync(item.Name);

        if (!string.IsNullOrWhiteSpace(newName))
            RenameGraph(item, newName);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _windowService?.ShowSettings();
    }

    public bool RenameGraph(GraphItem item, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return false;

        var trimmedName = newName.Trim();

        if (GraphItems.Any(x =>
                !ReferenceEquals(x, item) &&
                string.Equals(x.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
            return false;

        var oldName = item.Name;

        try
        {
            _repository.RenameGraph(oldName, trimmedName);

            item.Name = trimmedName;
            _repository.SaveGraphList(GraphItems);
            LoadGraphs();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка переименования графа {oldName} -> {trimmedName}: {ex.Message}");
            return false;
        }
    }

    private void LoadGraphs()
    {
        GraphItems.Clear();

        foreach (var item in _repository.GetGraphList())
            GraphItems.Add(item);
    }
}
