using CommunityToolkit.Mvvm.ComponentModel;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.ViewModels;

// ViewModel для одной строки списка. Оборачивает модель и добавляет
// уведомления об изменениях, на которые подписывается UI.
public partial class TodoItemViewModel : ObservableObject
{
    private readonly ITodoService _service;

    public TodoItemViewModel(TodoItem model, ITodoService service)
    {
        Model = model;
        _service = service;
        _isDone = model.IsDone; // пишем в поле напрямую, чтобы не сработал OnIsDoneChanged
    }

    public TodoItem Model { get; }

    public string Title => Model.Title;

    // [ObservableProperty] — source generator сам создаст публичное свойство IsDone
    // с вызовом PropertyChanged. Писать OnPropertyChanged вручную не нужно.
    [ObservableProperty]
    private bool _isDone;

    // Частичный метод-хук, его тоже генерирует тулкит: вызывается после смены IsDone.
    // Сохраняем в БД сразу при клике по чекбоксу.
    async partial void OnIsDoneChanged(bool value)
    {
        Model.IsDone = value;
        await _service.UpdateAsync(Model);
    }
}
