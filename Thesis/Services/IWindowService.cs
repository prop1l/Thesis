using System.Threading.Tasks;

namespace Thesis.Services;

/// <summary>
/// Открытие окон и диалогов. ViewModel вызывает этот сервис вместо того,
/// чтобы code-behind доставал окна из DI-контейнера.
/// </summary>
public interface IWindowService
{
    void ShowSettings();
    Task ShowGraphEditorAsync(string graphName);

    /// <summary>Возвращает новое имя или null, если пользователь нажал «Отмена».</summary>
    Task<string?> ShowRenameGraphDialogAsync(string currentName);
}
