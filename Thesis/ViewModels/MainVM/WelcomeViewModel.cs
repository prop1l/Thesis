using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    public ObservableCollection<GraphCardViewModel> GraphItems { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddGraphCommand))]
    [NotifyPropertyChangedFor(nameof(NameError), nameof(HasNameError))]
    private string newGraphName = string.Empty;

    public bool IsDark => _themeService?.IsDark ?? false;

    public bool IsEmpty => GraphItems.Count == 0;

    public string GraphCountText =>
        $"{GraphItems.Count} {GraphKindText.Plural(GraphItems.Count, "граф", "графа", "графов")}";

    /// <summary>Подсказка под полем ввода, если имя занято.</summary>
    public string? NameError => IsNameTaken(NewGraphName) ? "Граф с таким названием уже есть" : null;
    public bool HasNameError => NameError is not null;

    public WelcomeViewModel(
        IGraphRepository repository,
        IWindowService? windowService = null,
        IThemeService? themeService = null)
    {
        _repository = repository;
        _windowService = windowService;
        _themeService = themeService;

        GraphItems.CollectionChanged += OnGraphItemsChanged;
        LoadGraphs();

        if (_themeService != null)
        {
            _themeService.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IThemeService.IsDark))
                    OnPropertyChanged(nameof(IsDark));
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
        var data = _repository.CreateEmptyGraph(trimmedName);

        var card = new GraphCardViewModel(new GraphItem { Name = trimmedName });
        card.Update(data);
        GraphItems.Add(card);

        SaveGraphList();
        _repository.SaveGraph(trimmedName, data);

        NewGraphName = string.Empty;
    }

    private bool CanAddGraph()
        => !string.IsNullOrWhiteSpace(NewGraphName) && !IsNameTaken(NewGraphName);

    [RelayCommand]
    private async Task DeleteGraphAsync(GraphCardViewModel? card)
    {
        if (card is null)
            return;

        if (_windowService is not null)
        {
            var confirmed = await _windowService.ConfirmAsync(
                "Удалить граф?",
                $"Граф «{card.Name}» будет удалён без возможности восстановления.",
                "Удалить");

            if (!confirmed)
                return;
        }

        if (!GraphItems.Remove(card))
            return;

        SaveGraphList();
        _repository.DeleteGraph(card.Name);
    }

    [RelayCommand]
    private async Task OpenGraphAsync(GraphCardViewModel? card)
    {
        if (card is null || _windowService is null)
            return;

        await _windowService.ShowGraphEditorAsync(card.Name);

        // После закрытия редактора обновляем сводку на карточке
        card.Update(_repository.LoadGraph(card.Name));
    }

    [RelayCommand]
    private async Task RenameGraphAsync(GraphCardViewModel? card)
    {
        if (card is null || _windowService is null)
            return;

        var takenNames = GraphItems.Where(x => !ReferenceEquals(x, card)).Select(x => x.Name).ToList();
        var newName = await _windowService.ShowRenameGraphDialogAsync(card.Name, takenNames);

        if (!string.IsNullOrWhiteSpace(newName))
            RenameGraph(card, newName);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _windowService?.ShowSettings();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService?.ToggleLightDark();
    }

    public bool RenameGraph(GraphCardViewModel card, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return false;

        var trimmedName = newName.Trim();

        if (GraphItems.Any(x =>
                !ReferenceEquals(x, card) &&
                string.Equals(x.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
            return false;

        var oldName = card.Name;

        try
        {
            _repository.RenameGraph(oldName, trimmedName);

            card.Name = trimmedName;
            SaveGraphList();
            card.Update(_repository.LoadGraph(trimmedName));
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка переименования графа {oldName} -> {trimmedName}: {ex.Message}");
            return false;
        }
    }

    private bool IsNameTaken(string name) =>
        !string.IsNullOrWhiteSpace(name) &&
        GraphItems.Any(x => string.Equals(x.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

    private void SaveGraphList() => _repository.SaveGraphList(GraphItems.Select(x => x.Item));

    private void LoadGraphs()
    {
        GraphItems.Clear();

        foreach (var item in _repository.GetGraphList())
        {
            var card = new GraphCardViewModel(item);
            card.Update(_repository.LoadGraph(item.Name));
            GraphItems.Add(card);
        }
    }

    private void OnGraphItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(GraphCountText));
        OnPropertyChanged(nameof(NameError));
        OnPropertyChanged(nameof(HasNameError));
        AddGraphCommand.NotifyCanExecuteChanged();
    }
}
