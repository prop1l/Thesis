using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Data;
using TodoApp.Services;
using TodoApp.ViewModels;
using TodoApp.Views;

namespace TodoApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 1. Composition root: здесь регистрируются все зависимости приложения.
        var services = ConfigureServices();

        // 2. Создаём таблицы при первом запуске. В реальном проекте вместо этого
        //    используют миграции: dotnet ef migrations add ...
        using (var db = services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext())
            db.Database.EnsureCreated();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 3. Контейнер сам собирает MainWindowViewModel и подставляет ITodoService в конструктор.
            var vm = services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
            vm.LoadCommand.Execute(null);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        // База лежит в %LOCALAPPDATA%\TodoApp\todo.db (на Linux/macOS — аналогичная папка).
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TodoApp");
        Directory.CreateDirectory(dir);
        var dbPath = Path.Combine(dir, "todo.db");

        return new ServiceCollection()
            .AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"))
            .AddSingleton<ITodoService, TodoService>()
            .AddTransient<MainWindowViewModel>()
            .BuildServiceProvider();
    }
}
