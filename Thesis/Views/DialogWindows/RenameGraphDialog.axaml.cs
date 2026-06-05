using Avalonia.Controls;
using Avalonia.Interactivity;
using Thesis.ViewModels;

namespace Thesis.Views;

public partial class RenameGraphDialog : Window
{
    public RenameGraphDialog()
    {
        InitializeComponent();
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RenameGraphDialogViewModel vm && vm.CanSave)
        {
            Close(vm.GraphName);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}