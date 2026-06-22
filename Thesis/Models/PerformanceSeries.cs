using System.Collections.ObjectModel;

namespace Thesis.Models;

public class PerformanceSeries
{
    public string Title { get; set; } = string.Empty;
    public ObservableCollection<PerformancePoint> Points { get; set; } = new();
}