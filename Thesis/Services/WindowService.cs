using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Thesis.ViewModels;
using Thesis.Views;

namespace Thesis.Services;

public class WindowService(IServiceProvider services) : IWindowService
{
    private static Window? MainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public void ShowSettings()
    {
        var window = services.GetRequiredService<SettingWindow>();
        window.DataContext = services.GetRequiredService<SettingWindowViewModel>();
        window.Show();
    }

    public async Task ShowGraphEditorAsync(string graphName)
    {
        var viewModel = services.GetRequiredService<GraphEditorViewModel>();
        viewModel.LoadGraph(graphName);

        var window = services.GetRequiredService<GraphEditorWindow>();
        window.DataContext = viewModel;

        if (MainWindow is { } owner)
            await window.ShowDialog(owner);
        else
            window.Show();
    }

    public async Task<string?> ShowRenameGraphDialogAsync(string currentName)
    {
        var dialog = new RenameGraphDialog
        {
            DataContext = new RenameGraphDialogViewModel { GraphName = currentName }
        };

        return MainWindow is { } owner
            ? await dialog.ShowDialog<string?>(owner)
            : null;
    }
}
