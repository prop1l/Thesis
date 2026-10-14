using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using CommunityToolkit.Mvvm.ComponentModel;
using Thesis.Models;

namespace Thesis.Services;

public partial class ThemeService : ObservableObject, IThemeService
{
    private readonly ISettingsStore _store;

    [ObservableProperty]
    private ThemeMode mode;

    [ObservableProperty]
    private Color accentColor;

    [ObservableProperty]
    private bool isDark;

    public IReadOnlyList<Color> AccentPresets { get; } = new[]
    {
        "#4F46E5", // индиго
        "#2563EB", // синий
        "#0891B2", // бирюзовый
        "#059669", // изумрудный
        "#D97706", // янтарный
        "#E11D48", // розовый
        "#9333EA", // фиолетовый
        "#475569"  // графитовый
    }.Select(Color.Parse).ToList();

    public ThemeService(ISettingsStore store)
    {
        _store = store;

        var settings = store.Load();
        mode = settings.Theme;
        accentColor = Color.TryParse(settings.AccentColor, out var color) ? color : AccentPresets[0];

        if (Application.Current is { } app)
        {
            app.ActualThemeVariantChanged += (_, _) => IsDark = app.ActualThemeVariant == ThemeVariant.Dark;
            ApplyMode();
            ApplyAccent();
            IsDark = app.ActualThemeVariant == ThemeVariant.Dark;
        }
    }

    public void ToggleLightDark()
    {
        Mode = IsDark ? ThemeMode.Light : ThemeMode.Dark;
    }

    partial void OnModeChanged(ThemeMode value)
    {
        ApplyMode();
        Save();
    }

    partial void OnAccentColorChanged(Color value)
    {
        ApplyAccent();
        Save();
    }

    private void ApplyMode()
    {
        if (Application.Current is not { } app)
            return;

        // ThemeVariant.Default — следовать теме Windows
        app.RequestedThemeVariant = Mode switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void ApplyAccent()
    {
        var fluent = Application.Current?.Styles.OfType<FluentTheme>().FirstOrDefault();
        if (fluent is null)
            return;

        SetPaletteAccent(fluent, ThemeVariant.Light, ForLightTheme(AccentColor));
        SetPaletteAccent(fluent, ThemeVariant.Dark, ForDarkTheme(AccentColor));
    }

    private static void SetPaletteAccent(FluentTheme fluent, ThemeVariant variant, Color accent)
    {
        if (fluent.Palettes.TryGetValue(variant, out var palette))
            palette.Accent = accent;
        else
            fluent.Palettes[variant] = new ColorPaletteResources { Accent = accent };
    }

    // На светлом фоне акцент не должен быть слишком светлым, на тёмном — слишком тёмным,
    // иначе текст и иконки акцентного цвета плохо читаются
    public static Color ForLightTheme(Color color)
    {
        var hsl = color.ToHsl();
        return new HslColor(1, hsl.H, hsl.S, Math.Min(hsl.L, 0.5)).ToRgb();
    }

    public static Color ForDarkTheme(Color color)
    {
        var hsl = color.ToHsl();
        return new HslColor(1, hsl.H, Math.Min(hsl.S, 0.85), Math.Max(hsl.L, 0.68)).ToRgb();
    }

    private void Save()
    {
        _store.Save(new AppSettings
        {
            Theme = Mode,
            AccentColor = AccentColor.ToString()
        });
    }
}
