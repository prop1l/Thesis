using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Thesis.ViewModels.Dialogs;
using Thesis.ViewModels.Editor;
using Thesis.ViewModels.Settings;
using Thesis.Views.Dialogs;
using Thesis.Views.Editor;
using Thesis.Views.Settings;

namespace Thesis.Services;

public class WindowService(IServiceProvider services) : IWindowService
{
    private static IClassicDesktopStyleApplicationLifetime? Desktop =>
        Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    // Владелец диалога — активное окно (например, редактор), иначе главное
    private static Window? Owner =>
        Desktop?.Windows.FirstOrDefault(w => w.IsActive) ?? Desktop?.MainWindow;

    public void ShowSettings()
    {
        // Окно настроек одно: повторный клик просто выводит его на передний план
        var existing = Desktop?.Windows.OfType<SettingWindow>().FirstOrDefault();
        if (existing is not null)
        {
            existing.Activate();
            return;
        }

        var window = services.GetRequiredService<SettingWindow>();
        window.DataContext = services.GetRequiredService<SettingWindowViewModel>();

        if (Owner is { } owner)
            window.Show(owner);
        else
            window.Show();
    }

    public async Task ShowGraphEditorAsync(string graphName)
    {
        var viewModel = services.GetRequiredService<GraphEditorViewModel>();
        viewModel.LoadGraph(graphName);

        var window = services.GetRequiredService<GraphEditorWindow>();
        window.DataContext = viewModel;

        if (Owner is { } owner)
            await window.ShowDialog(owner);
        else
            window.Show();
    }

    public async Task<string?> ShowRenameGraphDialogAsync(string currentName, IReadOnlyCollection<string> takenNames)
    {
        var dialog = new RenameGraphDialog
        {
            DataContext = new RenameGraphDialogViewModel(takenNames) { GraphName = currentName }
        };

        return Owner is { } owner
            ? await dialog.ShowDialog<string?>(owner)
            : null;
    }

    public async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        var dialog = new ConfirmDialog
        {
            DataContext = new ConfirmDialogViewModel(title, message, confirmText)
        };

        return Owner is { } owner && await dialog.ShowDialog<bool>(owner);
    }
}
