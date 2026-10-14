using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media;
using Thesis.Models;

namespace Thesis.Services;

/// <summary>
/// Тема приложения. Интерфейс нужен, чтобы ViewModel-и можно было тестировать
/// без запущенного Avalonia-приложения.
/// </summary>
public interface IThemeService : INotifyPropertyChanged
{
    /// <summary>Выбранный режим: системная, светлая или тёмная тема.</summary>
    ThemeMode Mode { get; set; }

    /// <summary>Базовый акцентный цвет. Для тёмной темы автоматически берётся более светлый оттенок.</summary>
    Color AccentColor { get; set; }

    /// <summary>Фактически активна тёмная тема (с учётом системной).</summary>
    bool IsDark { get; }

    IReadOnlyList<Color> AccentPresets { get; }

    /// <summary>Быстрое переключение светлая ↔ тёмная (кнопка в шапке).</summary>
    void ToggleLightDark();
}
