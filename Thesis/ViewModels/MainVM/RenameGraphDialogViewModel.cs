using Avalonia.Media.TextFormatting.Unicode;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Thesis.ViewModels;

public partial class RenameGraphDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string graphName = string.Empty;

    public bool CanSave => !string.IsNullOrWhiteSpace(GraphName);

    partial void OnGraphNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
    }
}