using System;
using System.ComponentModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using Thesis.Models.Graph;

namespace Thesis.ViewModels.Editor;

/// <summary>
/// Вершина на холсте: данные модели плюс состояние отображения
/// (выделение, шаг алгоритма Дейкстры). Модель Node остаётся «чистой» и только сохраняется.
/// </summary>
public partial class NodeViewModel : ObservableObject
{
    private readonly Node _node;
    private readonly NodeStyle _style;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCurrent), nameof(ShowPath), nameof(ShowFrontier), nameof(ShowVisited), nameof(IsHighlighted))]
    private bool isCurrent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPath), nameof(ShowFrontier), nameof(ShowVisited), nameof(IsHighlighted))]
    private bool isOnPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowVisited))]
    private bool isVisited;

    /// <summary>Вершина ждёт обработки: в очереди, стеке или открытом множестве алгоритма.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFrontier), nameof(ShowVisited))]
    private bool isInFrontier;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText), nameof(FontSize))]
    private string distanceLabel = string.Empty;

    public NodeViewModel(Node node, NodeStyle style)
    {
        _node = node;
        _style = style;
        _style.PropertyChanged += OnStyleChanged;
    }

    public Node Model => _node;

    public string Id => _node.Id;

    public string Label
    {
        get => _node.Label;
        set
        {
            if (SetProperty(_node.Label, value, _node, (m, v) => m.Label = v))
                OnPropertyChanged(nameof(DisplayText));
        }
    }

    public double X
    {
        get => _node.X;
        set
        {
            if (SetProperty(_node.X, value, _node, (m, v) => m.X = v))
                OnPropertyChanged(nameof(Center));
        }
    }

    public double Y
    {
        get => _node.Y;
        set
        {
            if (SetProperty(_node.Y, value, _node, (m, v) => m.Y = v))
                OnPropertyChanged(nameof(Center));
        }
    }

    public double Width => _style.Width;
    public double Height => _style.Height;
    public Point Center => new(X + Width / 2, Y + Height / 2);

    public bool IsHighlighted => IsCurrent || IsOnPath;

    // Взаимоисключающие состояния алгоритма — к ним привязаны классы стилей в XAML.
    // Выделение (IsSelected) рисуется отдельно — акцентной обводкой поверх любого состояния.
    public bool ShowCurrent => IsCurrent;
    public bool ShowPath => !IsCurrent && IsOnPath;
    public bool ShowFrontier => !IsCurrent && !IsOnPath && IsInFrontier;
    public bool ShowVisited => !IsCurrent && !IsOnPath && !IsInFrontier && IsVisited;

    public string DisplayText => string.IsNullOrWhiteSpace(DistanceLabel)
        ? Label
        : $"{Label}\n{DistanceLabel}";

    public double FontSize => string.IsNullOrWhiteSpace(DistanceLabel)
        ? _style.FontSize
        : Math.Max(11, _style.FontSize - 1);

    public bool Contains(Point p) =>
        p.X >= X && p.X <= X + Width &&
        p.Y >= Y && p.Y <= Y + Height;

    public void ResetAlgorithmState()
    {
        IsVisited = false;
        IsCurrent = false;
        IsOnPath = false;
        IsInFrontier = false;
        DistanceLabel = string.Empty;
    }

    public void Detach() => _style.PropertyChanged -= OnStyleChanged;

    private void OnStyleChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(NodeStyle.Width):
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Center));
                break;
            case nameof(NodeStyle.Height):
                OnPropertyChanged(nameof(Height));
                OnPropertyChanged(nameof(Center));
                break;
            case nameof(NodeStyle.FontSize):
                OnPropertyChanged(nameof(FontSize));
                break;
        }
    }
}
