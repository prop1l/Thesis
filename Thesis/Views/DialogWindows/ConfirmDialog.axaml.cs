using System;
using Avalonia.Controls;
using Thesis.ViewModels;

namespace Thesis.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ConfirmDialogViewModel vm)
            vm.CloseRequested += result => Close(result);
    }
}
