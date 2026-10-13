using System.Collections.Generic;
using System.Threading.Tasks;
using Thesis.Services;

namespace Thesis.Tests;

/// <summary>Подменяет окна в тестах: запоминает вызовы и возвращает заранее заданный ответ.</summary>
public sealed class FakeWindowService : IWindowService
{
    public string? RenameDialogResult { get; set; }
    public List<string> OpenedEditors { get; } = new();
    public int SettingsShownCount { get; private set; }

    public void ShowSettings() => SettingsShownCount++;

    public Task ShowGraphEditorAsync(string graphName)
    {
        OpenedEditors.Add(graphName);
        return Task.CompletedTask;
    }

    public Task<string?> ShowRenameGraphDialogAsync(string currentName) =>
        Task.FromResult(RenameDialogResult);
}
