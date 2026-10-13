using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System;
using Thesis.ViewModels;

namespace Thesis.Views;

/// <summary>
/// Холст рисуется привязками в XAML. Здесь остались только вещи, которые по природе
/// относятся к View: перевод событий мыши в координаты холста и прокрутка ScrollViewer.
/// </summary>
public partial class GraphEditorWindow : Window
{
    private GraphEditorViewModel? Vm => DataContext as GraphEditorViewModel;

    public GraphEditorWindow()
    {
        InitializeComponent();

        GraphCanvas.PointerPressed += OnPointerPressed;
        GraphCanvas.PointerMoved += OnPointerMoved;
        GraphCanvas.PointerReleased += OnPointerReleased;
        GraphCanvas.DoubleTapped += (_, e) =>
        {
            Vm?.AddNodeAtCenter(e.GetPosition(GraphCanvas));
            e.Handled = true;
        };

        Opened += (_, _) => Dispatcher.UIThread.Post(CenterView, DispatcherPriority.Loaded);
        Closed += (_, _) => Vm?.CancelEdgeCreation();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null)
            return;

        var point = e.GetCurrentPoint(GraphCanvas);

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
        Vm?.OnCanvasPointerMoved(e.GetPosition(GraphCanvas));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        Vm?.OnCanvasPointerReleased();
    }

    private void CenterView()
    {
        if (Vm?.GetGraphCenter() is not { } center)
            return;

        GraphScrollViewer.Offset = new Vector(
            Math.Max(0, center.X - GraphScrollViewer.Bounds.Width / 2),
            Math.Max(0, center.Y - GraphScrollViewer.Bounds.Height / 2));
    }
}
