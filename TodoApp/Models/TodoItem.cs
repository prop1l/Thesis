namespace TodoApp.Models;

// Модель (M в MVVM) — обычный POCO-класс, ничего не знает про UI.
// EF Core сам сопоставит его с таблицей TodoItems в SQLite.
public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsDone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
