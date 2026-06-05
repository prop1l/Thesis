using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Thesis.Services;

public partial class ThemeService : ObservableObject
{
    private readonly ColorPaletteResources _lightPalette = App.LightPalette;
    private readonly ColorPaletteResources _darkPalette = App.DarkPalette;

    [ObservableProperty]
    private bool isDarkTheme;

    [ObservableProperty]
    private Color accentColor = Colors.Blue;

    public ThemeService()
    {
        if (Application.Current is { } app)
            IsDarkTheme = app.ActualThemeVariant == ThemeVariant.Dark;

        AccentColor = _lightPalette.Accent;
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    partial void OnAccentColorChanged(Color value)
    {
        _lightPalette.Accent = value;
        _darkPalette.Accent = value;
    }
}