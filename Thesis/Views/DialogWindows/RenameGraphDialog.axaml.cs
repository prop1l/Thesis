using System;
using Avalonia.Controls;
using Thesis.ViewModels;

namespace Thesis.Views;

public partial class RenameGraphDialog : Window
{
    public RenameGraphDialog()
    {
        InitializeComponent();

        // Сразу ставим курсор в поле и выделяем старое имя — его обычно заменяют целиком
        Opened += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is RenameGraphDialogViewModel vm)
            vm.CloseRequested += Close;
    }
}
