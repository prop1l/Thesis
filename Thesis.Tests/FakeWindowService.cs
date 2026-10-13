using System.Collections.Generic;
using System.Threading.Tasks;
using Thesis.Services;

namespace Thesis.Tests;

/// <summary>Подменяет окна в тестах: запоминает вызовы и возвращает заранее заданный ответ.</summary>
public sealed class FakeWindowService : IWindowService
{
    public string? RenameDialogResult { get; set; }
    public bool ConfirmResult { get; set; } = true;
    public List<string> OpenedEditors { get; } = new();
    public int SettingsShownCount { get; private set; }
    public int ConfirmShownCount { get; private set; }

    public void ShowSettings() => SettingsShownCount++;

    public Task ShowGraphEditorAsync(string graphName)
    {
        OpenedEditors.Add(graphName);
        return Task.CompletedTask;
    }

    public Task<string?> ShowRenameGraphDialogAsync(string currentName, IReadOnlyCollection<string> takenNames) =>
        Task.FromResult(RenameDialogResult);

    public Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        ConfirmShownCount++;
        return Task.FromResult(ConfirmResult);
    }
}
