using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Thesis.Services;

namespace Thesis.ViewModels;
 
public partial class SettingWindowViewModel : ObservableObject
{
    private readonly ThemeService _themeService;

    [ObservableProperty]
    private bool useCompactDensity = true;

    [ObservableProperty]
    private bool useAnimations = true;

    [ObservableProperty]
    private string userName = "Developer";

    [ObservableProperty]
    private bool notificationsEnabled = true;

    [ObservableProperty]
    private bool playNotificationSound = false;

    [ObservableProperty]
    private bool showStartupTips = true;

    public bool IsDarkTheme
    {
        get => _themeService.IsDarkTheme;
        set => _themeService.IsDarkTheme = value;
    }

    public Color AccentColor
    {
        get => _themeService.AccentColor;
        set => _themeService.AccentColor = value;
    }

    public SettingWindowViewModel(ThemeService themeService)
    {
        _themeService = themeService;

        _themeService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ThemeService.IsDarkTheme))
                OnPropertyChanged(nameof(IsDarkTheme));

            if (e.PropertyName == nameof(ThemeService.AccentColor))
                OnPropertyChanged(nameof(AccentColor));
        };
    }

    [RelayCommand]
    private void SetAccent(string colorHex)
    {
        AccentColor = Color.Parse(colorHex);
    }
}