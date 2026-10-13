using Avalonia.Controls;
using Avalonia.Input;
using Thesis.ViewModels;

namespace Thesis.Views;

public partial class Welcome : Window
{
    public Welcome()
    {
        InitializeComponent();
    }

    // У Image нет свойства Command, поэтому просто передаём нажатие во ViewModel
    private void Image_Tapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is WelcomeViewModel vm)
            vm.OpenSettingsCommand.Execute(null);
    }
}
