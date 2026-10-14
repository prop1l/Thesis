using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Services;

// В desktop-приложении DbContext не держат открытым всё время:
// на каждую операцию через фабрику создаётся короткоживущий контекст.
public class TodoService(IDbContextFactory<AppDbContext> dbFactory) : ITodoService
{
    public async Task<List<TodoItem>> GetAllAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.TodoItems.OrderBy(t => t.IsDone).ThenByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<TodoItem> AddAsync(string title)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var item = new TodoItem { Title = title };
        db.TodoItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(TodoItem item)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.TodoItems.Update(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.TodoItems.Where(t => t.Id == id).ExecuteDeleteAsync();
    }
}
