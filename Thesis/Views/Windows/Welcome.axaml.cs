using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using Thesis.Models.Graph;
using Thesis.Services;
using Thesis.ViewModels;

namespace Thesis.Views;

public partial class Welcome : Window
{
    public Welcome()
    {
        InitializeComponent();
    }

    private void ThemeToggle_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && DataContext is WelcomeViewModel vm)
        {
            vm.IsDarkTheme = toggle.IsChecked == true;
        }
    }

    private void Image_Tapped(object? sender, TappedEventArgs e)
    {
        if (Application.Current is App app && app.Services != null)
        {
            var settingsWindow = app.Services.GetRequiredService<SettingWindow>();
            var themeService = app.Services.GetRequiredService<ThemeService>();
            settingsWindow.DataContext = new SettingWindowViewModel(themeService);
            settingsWindow.Show();
        }
    }

    private async void GraphCard_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is GraphItem graphItem &&
            Application.Current is App app && app.Services != null)
        {
            var editorWindow = app.Services.GetRequiredService<GraphEditorWindow>();
            var editorVm = app.Services.GetRequiredService<GraphEditorViewModel>();

            editorVm.LoadGraph(graphItem.Name);
            editorWindow.DataContext = editorVm;

            //editorVm.Refresh();

            await editorWindow.ShowDialog(this);
        }
    }

    private async void RenameGraphMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem ||
            menuItem.CommandParameter is not GraphItem item ||
            DataContext is not WelcomeViewModel welcomeVm)
            return;

        var dialogVm = new RenameGraphDialogViewModel
        {
            GraphName = item.Name
        };

        var dialog = new RenameGraphDialog
        {
            DataContext = dialogVm
        };

        var result = await dialog.ShowDialog<string?>(this);

        if (!string.IsNullOrWhiteSpace(result))
        {
            welcomeVm.RenameGraph(item, result);
        }
    }
}