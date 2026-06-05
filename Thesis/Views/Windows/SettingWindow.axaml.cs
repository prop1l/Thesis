using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Thesis.ViewModels;

namespace Thesis.Views;

public partial class SettingWindow : Window
{
    public SettingWindow()
    {
        InitializeComponent();
        InitializeSectionBrushes();
    }

    private void InitializeSectionBrushes()
    {
        var defaultBrush = GetDefaultSectionBrush();

        AppearanceSection.Background = defaultBrush;
        AccentSection.Background = defaultBrush;
    }

    private IBrush GetDefaultSectionBrush()
    {
        if (ActualThemeVariant == ThemeVariant.Dark)
            return new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));

        return new SolidColorBrush(Color.FromArgb(0x10, 0x00, 0x00, 0x00));
    }

    private IBrush GetHighlightSectionBrush()
    {
        if (ActualThemeVariant == ThemeVariant.Dark)
            return new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));

        return new SolidColorBrush(Color.FromArgb(0x28, 0x00, 0x00, 0x00));
    }

    private void ScrollToAppearanceSection(object? sender, RoutedEventArgs e)
    {
        HighlightSection(AppearanceSection);
        AppearanceSection.BringIntoView();
    }

    private void ScrollToAccentSection(object? sender, RoutedEventArgs e)
    {
        HighlightSection(AccentSection);
        AccentSection.BringIntoView();
    }

    private void HighlightSection(Border section)
    {
        var defaultBrush = GetDefaultSectionBrush();
        var highlightBrush = GetHighlightSectionBrush();

        AppearanceSection.Background = defaultBrush;
        AccentSection.Background = defaultBrush;

        section.Background = highlightBrush;

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(0.3)
        };

        timer.Tick += (_, _) =>
        {
            section.Background = defaultBrush;
            timer.Stop();
        };

        timer.Start();
    }

    private void ThemeToggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && DataContext is SettingWindowViewModel vm)
        {
            vm.IsDarkTheme = toggle.IsChecked == true;
            InitializeSectionBrushes();
        }
    }
}