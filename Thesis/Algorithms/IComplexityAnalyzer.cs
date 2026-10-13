using System.Collections.Generic;
using Thesis.Models;

namespace Thesis.Algorithms;

public sealed record ComplexityReport(IReadOnlyList<PerformancePoint> Points, string Summary);

public interface IComplexityAnalyzer
{
    /// <summary>Строит данные для графика теоретической сложности алгоритма Дейкстры.</summary>
    ComplexityReport BuildComplexityReport();
}
