using Avalonia.Controls;
using Avalonia.Controls.Shapes;

namespace Thesis.Views;

public sealed record NodeVisual(Ellipse Ellipse, TextBlock Text);
public sealed record EdgeVisual(Line Line, Line ArrowLeft,
    Line ArrowRight, Border WeightBorder, TextBlock WeightText);