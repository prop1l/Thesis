using Avalonia.Media;

namespace Thesis.Helpers;

public static class BrushHelper
{
    public static IBrush CreateBrush(string color)
    {
        try { return Brush.Parse(color); }
        catch { return Brushes.Gray; }
    }
}