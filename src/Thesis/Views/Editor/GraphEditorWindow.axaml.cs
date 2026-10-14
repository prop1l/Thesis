using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.ComponentModel;
using Thesis.ViewModels.Editor;

namespace Thesis.Views.Editor;

/// <summary>
/// Холст рисуется привязками в XAML. Здесь остались только вещи, которые по природе
/// относятся к View: перевод событий мыши в координаты графа, масштаб и прокрутка.
/// </summary>
/// <remarks>
/// Холст бесконечный, как в draw.io. Граф живёт в «мировых» координатах (могут быть отрицательными),
/// а GraphCanvas — это лишь кусок мира, который сейчас можно прокрутить: область с вершинами
/// плюс запас в один экран с каждой стороны, объединённая с тем, что видно в окне.
/// Когда кусок меняется (вершину утащили за край, долистали до края, сменили масштаб),
/// прокрутка сдвигается на ту же величину — картинка на экране при этом не прыгает.
/// </remarks>
public partial class GraphEditorWindow : Window
{
    // Шаг сетки фона (CanvasGridBrush). Начало холста кратно ему, иначе сетка «поплывёт» при росте холста
    private const double GridStep = 24;

    private GraphEditorViewModel? Vm => DataContext as GraphEditorViewModel;

    private GraphEditorViewModel? _subscribedVm;
    private double _appliedZoom = 1.0;

    // Мировые координаты левого верхнего угла GraphCanvas
    private Point _canvasOrigin;

    // Точка окна, которая должна остаться на месте при следующем изменении масштаба.
    // Колёсико задаёт позицию курсора; кнопки и Ctrl ± — центр (null)
    private Point? _zoomAnchor;

    // Перемещение холста зажатым колёсиком: где нажали и какая область мира была видна в этот момент
    private bool _isPanning;
    private Point _panStartPointer;
    private Point _panStartWorld;

    private bool _isUpdatingCanvas;

    public GraphEditorWindow()
    {
        InitializeComponent();

        // Tunnel: обрабатываем раньше ScrollViewer, иначе он прокрутит холст вместо масштабирования
        GraphScrollViewer.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        DataContextChanged += (_, _) => SubscribeToViewModel();

        // Tunnel и захват указателя: пока холст тащат колёсиком, вершины не выделяются и не двигаются
        GraphScrollViewer.AddHandler(PointerPressedEvent, OnPanPointerPressed, RoutingStrategies.Tunnel);
        GraphScrollViewer.AddHandler(PointerMovedEvent, OnPanPointerMoved, RoutingStrategies.Tunnel);
        GraphScrollViewer.AddHandler(PointerReleasedEvent, OnPanPointerReleased, RoutingStrategies.Tunnel);
        GraphScrollViewer.PointerCaptureLost += (_, _) => EndPan();

        GraphScrollViewer.SizeChanged += (_, _) => UpdateCanvas();
        GraphScrollViewer.ScrollChanged += OnScrollChanged;

        GraphCanvas.PointerPressed += OnPointerPressed;
        GraphCanvas.PointerMoved += OnPointerMoved;
        GraphCanvas.PointerReleased += OnPointerReleased;
        GraphCanvas.DoubleTapped += (_, e) =>
        {
            Vm?.AddNodeAtCenter(e.GetPosition(GraphWorld));
            e.Handled = true;
        };

        Opened += (_, _) => Dispatcher.UIThread.Post(CenterView, DispatcherPriority.Loaded);
        Closed += (_, _) =>
        {
            Vm?.CancelEdgeCreation();
            if (_subscribedVm is not null)
                _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
        };
    }

    #region Mouse on graph

    // Позиции берутся относительно GraphWorld — это сразу мировые координаты графа

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null)
            return;

        var point = e.GetCurrentPoint(GraphWorld);

        if (point.Properties.IsRightButtonPressed)
        {
            Vm.OnCanvasRightPressed(point.Position);
            e.Handled = true;
        }
        else if (point.Properties.IsLeftButtonPressed)
        {
            Vm.OnCanvasLeftPressed(point.Position);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        Vm?.OnCanvasPointerMoved(e.GetPosition(GraphWorld));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        Vm?.OnCanvasPointerReleased();
    }

    #endregion

    #region Zoom and panning

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (Vm is null || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        _zoomAnchor = e.GetPosition(GraphScrollViewer);
        Vm.ZoomByWheel(e.Delta.Y);
        _zoomAnchor = null;
        e.Handled = true;
    }

    /// <summary>
    /// Меняет масштаб так, чтобы точка графа под якорем (курсором или центром окна)
    /// осталась на том же месте экрана.
    /// </summary>
    private void ApplyZoom(double newZoom)
    {
        var viewport = ViewportSize;
        var anchor = _zoomAnchor ?? new Point(viewport.Width / 2, viewport.Height / 2);
        var anchorWorld = ToWorld(anchor);

        _appliedZoom = newZoom;
        GraphZoom.LayoutTransform = new ScaleTransform(newZoom, newZoom);

        ShowWorldAt(anchorWorld - anchor / newZoom);
    }

    private void OnPanPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(GraphScrollViewer).Properties.IsMiddleButtonPressed)
            return;

        _isPanning = true;
        _panStartPointer = e.GetPosition(GraphScrollViewer);
        _panStartWorld = ToWorld(default);
        GraphScrollViewer.Cursor = new Cursor(StandardCursorType.SizeAll);
        e.Pointer.Capture(GraphScrollViewer);
        e.Handled = true;
    }

    private void OnPanPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning)
            return;

        // Позиция берётся относительно ScrollViewer, а не холста: холст сам сдвигается при прокрутке.
        // ShowWorldAt достраивает холст, поэтому тянуть можно сколько угодно далеко
        var delta = e.GetPosition(GraphScrollViewer) - _panStartPointer;
        ShowWorldAt(_panStartWorld - delta / _appliedZoom);
        e.Handled = true;
    }

    private void OnPanPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isPanning || e.InitialPressMouseButton != MouseButton.Middle)
            return;

        e.Pointer.Capture(null);
        EndPan();
        e.Handled = true;
    }

    private void EndPan()
    {
        _isPanning = false;
        GraphScrollViewer.Cursor = null;
    }

    /// <summary>
    /// Долистали колёсиком или полосой прокрутки до края — достраиваем холст ещё на экран в ту сторону,
    /// как это делает draw.io.
    /// </summary>
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_isUpdatingCanvas || _isPanning)
            return;

        var offset = GraphScrollViewer.Offset;
        var maxX = GraphScrollViewer.Extent.Width - GraphScrollViewer.Viewport.Width;
        var maxY = GraphScrollViewer.Extent.Height - GraphScrollViewer.Viewport.Height;

        if (offset.X <= 0.5 || offset.Y <= 0.5 || offset.X >= maxX - 0.5 || offset.Y >= maxY - 0.5)
        {
            // Не прямо из обработчика: он вызывается во время раскладки, а UpdateCanvas её перезапускает
            Dispatcher.UIThread.Post(() => UpdateCanvas(growAtEdges: true), DispatcherPriority.Background);
        }
    }

    private void CenterView()
    {
        // Пустой граф: начало координат в левом верхнем углу окна
        var center = Vm?.ContentBounds?.Center
                     ?? new Point(ViewportSize.Width / 2 / _appliedZoom, ViewportSize.Height / 2 / _appliedZoom);

        ShowWorldAt(center - new Point(ViewportSize.Width / 2, ViewportSize.Height / 2) / _appliedZoom);
    }

    #endregion

    #region Canvas bounds

    private Size ViewportSize => GraphScrollViewer.Bounds.Size;

    /// <summary>Точка окна → мировые координаты графа.</summary>
    private Point ToWorld(Point viewportPoint)
    {
        var offset = GraphScrollViewer.Offset;
        return new Point(
            _canvasOrigin.X + (offset.X + viewportPoint.X) / _appliedZoom,
            _canvasOrigin.Y + (offset.Y + viewportPoint.Y) / _appliedZoom);
    }

    /// <summary>Пересчитать холст, не сдвигая картинку на экране.</summary>
    private void UpdateCanvas(bool growAtEdges = false)
    {
        var visible = new Rect(ToWorld(default), ViewportSize / _appliedZoom);

        if (growAtEdges)
        {
            var offset = GraphScrollViewer.Offset;
            var maxX = GraphScrollViewer.Extent.Width - GraphScrollViewer.Viewport.Width;
            var maxY = GraphScrollViewer.Extent.Height - GraphScrollViewer.Viewport.Height;
            var screen = visible.Size;

            // В сторону каждого края, до которого долистали, добавляем по экрану
            var left = offset.X <= 0.5 ? screen.Width : 0;
            var top = offset.Y <= 0.5 ? screen.Height : 0;
            var right = offset.X >= maxX - 0.5 ? screen.Width : 0;
            var bottom = offset.Y >= maxY - 0.5 ? screen.Height : 0;

            ResizeCanvas(visible, new Rect(
                visible.X - left, visible.Y - top,
                visible.Width + left + right, visible.Height + top + bottom));
            return;
        }

        ResizeCanvas(visible, visible);
    }

    /// <summary>Прокрутить так, чтобы левый верхний угол окна показывал эту точку мира.</summary>
    private void ShowWorldAt(Point worldTopLeft)
    {
        var visible = new Rect(worldTopLeft, ViewportSize / _appliedZoom);
        ResizeCanvas(visible, visible);
    }

    /// <summary>
    /// Холст = область с вершинами плюс экран запаса с каждой стороны, объединённая с mustInclude
    /// (то, что видно сейчас или должно стать видно). Затем прокрутка ставится так, чтобы в окне
    /// оказалась область visible.
    /// </summary>
    private void ResizeCanvas(Rect visible, Rect mustInclude)
    {
        if (ViewportSize.Width <= 0 || ViewportSize.Height <= 0)
            return;

        var bounds = mustInclude;

        if (Vm?.ContentBounds is { } content)
        {
            var margin = ViewportSize / _appliedZoom;
            bounds = bounds.Union(content.Inflate(new Thickness(margin.Width, margin.Height)));
        }

        // Края холста — по линиям сетки
        var left = Math.Floor(bounds.Left / GridStep) * GridStep;
        var top = Math.Floor(bounds.Top / GridStep) * GridStep;
        var right = Math.Ceiling(bounds.Right / GridStep) * GridStep;
        var bottom = Math.Ceiling(bounds.Bottom / GridStep) * GridStep;

        _isUpdatingCanvas = true;
        try
        {
            _canvasOrigin = new Point(left, top);
            GraphWorld.RenderTransform = new TranslateTransform(-left, -top);
            GraphCanvas.Width = right - left;
            GraphCanvas.Height = bottom - top;

            // Пересчитываем размеры сейчас, иначе новое смещение обрежется по старой области прокрутки
            GraphScrollViewer.UpdateLayout();

            GraphScrollViewer.Offset = new Vector(
                (visible.X - left) * _appliedZoom,
                (visible.Y - top) * _appliedZoom);
        }
        finally
        {
            _isUpdatingCanvas = false;
        }
    }

    #endregion

    #region View model

    private void SubscribeToViewModel()
    {
        if (_subscribedVm is not null)
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;

        _subscribedVm = Vm;

        if (_subscribedVm is not null)
        {
            _subscribedVm.PropertyChanged += OnViewModelPropertyChanged;
            _appliedZoom = _subscribedVm.ZoomLevel;
            GraphZoom.LayoutTransform = new ScaleTransform(_appliedZoom, _appliedZoom);
            UpdateCanvas();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Vm is not { } vm)
            return;

        if (e.PropertyName == nameof(GraphEditorViewModel.ZoomLevel))
            ApplyZoom(vm.ZoomLevel);
        else if (e.PropertyName == nameof(GraphEditorViewModel.ContentBounds))
            UpdateCanvas();
    }

    #endregion
}
