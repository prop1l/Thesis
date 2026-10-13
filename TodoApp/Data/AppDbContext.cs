using Microsoft.EntityFrameworkCore;
using TodoApp.Models;

namespace TodoApp.Data;

// DbContext — точка входа в базу данных через EF Core.
// Каждое свойство DbSet<T> соответствует таблице.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
}
