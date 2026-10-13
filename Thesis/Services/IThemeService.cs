using System.ComponentModel;
using Avalonia.Media;

namespace Thesis.Services;

/// <summary>
/// Тема приложения. Интерфейс нужен, чтобы ViewModel-и можно было тестировать
/// без запущенного Avalonia-приложения.
/// </summary>
public interface IThemeService : INotifyPropertyChanged
{
    bool IsDarkTheme { get; set; }
    Color AccentColor { get; set; }
}
