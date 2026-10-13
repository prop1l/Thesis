using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Thesis.ViewModels;

public partial class RenameGraphDialogViewModel : ObservableObject
{
    private readonly IReadOnlyCollection<string> _takenNames;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErrorText), nameof(HasError), nameof(CanSave))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string graphName = string.Empty;

    public RenameGraphDialogViewModel(IReadOnlyCollection<string>? takenNames = null)
    {
        _takenNames = takenNames ?? [];
    }

    /// <summary>Окно подписывается и закрывается с этим результатом (null — отмена).</summary>
    public event Action<string?>? CloseRequested;

    public string? ErrorText =>
        string.IsNullOrWhiteSpace(GraphName) ? "Введите название"
        : _takenNames.Any(n => string.Equals(n, GraphName.Trim(), StringComparison.OrdinalIgnoreCase))
            ? "Граф с таким названием уже есть"
            : null;

    public bool HasError => ErrorText is not null && !string.IsNullOrEmpty(GraphName);

    public bool CanSave => ErrorText is null;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save() => CloseRequested?.Invoke(GraphName.Trim());

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(null);
}
