using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Thesis.Services;
using Thesis.ViewModels;
using Thesis.Views;

namespace Thesis
{
    public partial class App : Application
    {
        public static ColorPaletteResources LightPalette { get; } = new()
        {
            Accent = Color.Parse("#0063B1")
        };

        public static ColorPaletteResources DarkPalette { get; } = new()
        {
            Accent = Color.Parse("#4CC2FF")
        };

        public IHost? Host { get; private set; }

        // Добавьте это свойство для доступа к сервисам
        public IServiceProvider? Services => Host?.Services;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            if (Styles[0] is FluentTheme fluentTheme)
            {
                fluentTheme.Palettes[ThemeVariant.Light] = LightPalette;
                fluentTheme.Palettes[ThemeVariant.Dark] = DarkPalette;
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        //Services
                        services.AddSingleton<ThemeService>();

                        //Windows
                        services.AddTransient<Welcome>();
                        services.AddTransient<SettingWindow>();
                        services.AddTransient<GraphEditorWindow>();

                        //ViewModels
                        services.AddTransient<WelcomeViewModel>();
                        services.AddTransient<SettingWindowViewModel>();
                        services.AddTransient<GraphEditorViewModel>();
                    })
                    .Build();

                Host.Start();

                var welcomeViewModel = Host.Services.GetRequiredService<WelcomeViewModel>();
                desktop.MainWindow = Host.Services.GetRequiredService<Welcome>();
                desktop.MainWindow.DataContext = welcomeViewModel;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}