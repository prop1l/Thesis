using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Thesis.Models;
using Thesis.Services;

namespace Thesis.ViewModels;

public partial class ThemeModeOption(ThemeMode mode, string title, string description) : ObservableObject
{
    public ThemeMode Mode { get; } = mode;
    public string Title { get; } = title;
    public string Description { get; } = description;

    [ObservableProperty]
    private bool isSelected;
}

public partial class AccentOption(Color color) : ObservableObject
{
    public Color Color { get; } = color;
    public IBrush Brush { get; } = new SolidColorBrush(color);

    [ObservableProperty]
    private bool isSelected;
}

public partial class SettingWindowViewModel : ObservableObject
{
    private readonly IThemeService _themeService;

    public IReadOnlyList<ThemeModeOption> ThemeModes { get; } =
    [
        new(ThemeMode.System, "Как в системе", "Следует теме Windows"),
        new(ThemeMode.Light, "Светлая", "Всегда светлая"),
        new(ThemeMode.Dark, "Тёмная", "Всегда тёмная")
    ];

    public IReadOnlyList<AccentOption> Accents { get; }

    public SettingWindowViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        Accents = themeService.AccentPresets.Select(c => new AccentOption(c)).ToList();

        _themeService.PropertyChanged += (_, _) => SyncSelection();
        SyncSelection();
    }

    [RelayCommand]
    private void SelectThemeMode(ThemeModeOption option)
    {
        _themeService.Mode = option.Mode;
    }

    [RelayCommand]
    private void SelectAccent(AccentOption option)
    {
        _themeService.AccentColor = option.Color;
    }

    private void SyncSelection()
    {
        foreach (var option in ThemeModes)
            option.IsSelected = option.Mode == _themeService.Mode;

        foreach (var option in Accents)
            option.IsSelected = option.Color == _themeService.AccentColor;
    }
}
