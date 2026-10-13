using System;
using CommunityToolkit.Mvvm.Input;

namespace Thesis.ViewModels;

public partial class ConfirmDialogViewModel(string title, string message, string confirmText)
{
    public string Title { get; } = title;
    public string Message { get; } = message;
    public string ConfirmText { get; } = confirmText;

    public event Action<bool>? CloseRequested;

    [RelayCommand]
    private void Confirm() => CloseRequested?.Invoke(true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
