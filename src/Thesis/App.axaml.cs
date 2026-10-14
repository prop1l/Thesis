using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Thesis.Algorithms;
using Thesis.Services;
using Thesis.ViewModels.Editor;
using Thesis.ViewModels.Settings;
using Thesis.ViewModels.Welcome;
using Thesis.Views.Editor;
using Thesis.Views.Settings;
using Thesis.Views.Welcome;

namespace Thesis
{
    public partial class App : Application
    {
        public IHost? Host { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        //Services
                        services.AddSingleton<ISettingsStore>(_ => new JsonSettingsStore(JsonGraphRepository.DefaultFolder));
                        services.AddSingleton<IThemeService, ThemeService>();
                        services.AddSingleton<IGraphRepository>(_ => new JsonGraphRepository(JsonGraphRepository.DefaultFolder));
                        services.AddSingleton<IWindowService, WindowService>();
                        services.AddSingleton(_ => PathAlgorithmCatalog.CreateAll());
                        services.AddSingleton<IComplexityAnalyzer, ComplexityAnalyzer>();

                        //Windows
                        services.AddTransient<WelcomeWindow>();
                        services.AddTransient<SettingWindow>();
                        services.AddTransient<GraphEditorWindow>();

                        //ViewModels
                        services.AddTransient<WelcomeViewModel>();
                        services.AddTransient<SettingWindowViewModel>();
                        services.AddTransient<GraphEditorViewModel>();
                    })
                    .Build();

                Host.Start();

                // Тему применяем до создания окон, чтобы не было «вспышки» светлой темы
                Host.Services.GetRequiredService<IThemeService>();
                desktop.Exit += (_, _) => Host.Dispose();

                var welcomeViewModel = Host.Services.GetRequiredService<WelcomeViewModel>();
                desktop.MainWindow = Host.Services.GetRequiredService<WelcomeWindow>();
                desktop.MainWindow.DataContext = welcomeViewModel;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}