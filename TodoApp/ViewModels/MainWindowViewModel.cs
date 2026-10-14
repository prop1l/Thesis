using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.ViewModels;

// Главная ViewModel. Здесь вся логика экрана, но ни одной ссылки на контролы Avalonia:
// её можно покрыть unit-тестами без запуска UI.
public partial class MainWindowViewModel(ITodoService service) : ObservableObject
{
    // ObservableCollection сама сообщает UI о добавлении и удалении элементов.
    public ObservableCollection<TodoItemViewModel> Items { get; } = [];

    // NotifyCanExecuteChangedFor: при изменении текста кнопка «Добавить»
    // перепроверяет CanAdd и включается или выключается.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private string _newTitle = "";

    [ObservableProperty]
    private string _status = "";

    // [RelayCommand] генерирует свойство LoadCommand (IAsyncRelayCommand).
    [RelayCommand]
    private async Task LoadAsync()
    {
        Items.Clear();
        foreach (var item in await service.GetAllAsync())
            Items.Add(CreateItemViewModel(item));
        UpdateStatus();
    }

    // Генерирует AddCommand. Пока CanAdd возвращает false, кнопка неактивна.
    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        var item = await service.AddAsync(NewTitle.Trim());
        Items.Insert(0, CreateItemViewModel(item));
        NewTitle = "";
        UpdateStatus();
    }

    private bool CanAdd() => !string.IsNullOrWhiteSpace(NewTitle);

    // Команда с параметром: в XAML передаём в неё конкретную строку списка.
    [RelayCommand]
    private async Task DeleteAsync(TodoItemViewModel item)
    {
        await service.DeleteAsync(item.Model.Id);
        Items.Remove(item);
        UpdateStatus();
    }

    private TodoItemViewModel CreateItemViewModel(TodoItem item)
    {
        var vm = new TodoItemViewModel(item, service);
        vm.PropertyChanged += (_, _) => UpdateStatus(); // пересчитать счётчик при клике по чекбоксу
        return vm;
    }

    private void UpdateStatus() =>
        Status = $"Выполнено {Items.Count(i => i.IsDone)} из {Items.Count}";
}
