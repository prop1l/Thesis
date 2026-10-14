using TodoApp.Models;

namespace TodoApp.Services;

// ViewModel зависит от интерфейса, а не от EF Core напрямую.
// Поэтому в тестах сервис легко подменить заглушкой.
public interface ITodoService
{
    Task<List<TodoItem>> GetAllAsync();
    Task<TodoItem> AddAsync(string title);
    Task UpdateAsync(TodoItem item);
    Task DeleteAsync(int id);
}
