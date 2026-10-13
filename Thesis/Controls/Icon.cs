using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Thesis.Controls;

/// <summary>
/// Линейная иконка: геометрия в системе координат 24×24, обводится цветом текста (Foreground).
/// Цвет наследуется от родителя, поэтому иконка в кнопке автоматически меняет цвет
/// вместе с темой, при наведении и в неактивном состоянии.
/// </summary>
public class Icon : Control
{
    private const double ViewBoxSize = 24;

    public static readonly StyledProperty<Geometry?> DataProperty =
        AvaloniaProperty.Register<Icon, Geometry?>(nameof(Data));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<Icon>();

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Icon, double>(nameof(StrokeThickness), 1.75);

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<Icon, double>(nameof(Size), 16);

    static Icon()
    {
        AffectsRender<Icon>(DataProperty, ForegroundProperty, StrokeThicknessProperty);
        AffectsMeasure<Icon>(SizeProperty);
    }

    public Geometry? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    public override void Render(DrawingContext context)
    {
        if (Data is null || Foreground is null)
            return;

        var scale = Size / ViewBoxSize;
        // Толщину линии компенсируем, чтобы она не зависела от размера иконки
        var pen = new Pen(Foreground, StrokeThickness / scale, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

        using (context.PushTransform(Matrix.CreateScale(scale, scale)))
            context.DrawGeometry(null, pen, Data);
    }
}
