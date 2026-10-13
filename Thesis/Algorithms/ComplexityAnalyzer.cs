using System;
using System.Collections.Generic;
using System.Text;
using Thesis.Models;

namespace Thesis.Algorithms;

public class ComplexityAnalyzer : IComplexityAnalyzer
{
    private static readonly int[] TestSizes = [5, 10, 20, 30, 50, 75, 100];

    public ComplexityReport BuildComplexityReport()
    {
        var points = new List<PerformancePoint>();

        foreach (var vertexCount in TestSizes)
        {
            var edgeCount = vertexCount * (vertexCount - 1) / 2;

            points.Add(new PerformancePoint
            {
                VertexCount = vertexCount,
                EdgeCount = edgeCount,
                TheoreticalTime = vertexCount * Math.Log(vertexCount),
                Complexity = "O(V log V)"
            });

            points.Add(new PerformancePoint
            {
                VertexCount = vertexCount,
                EdgeCount = edgeCount,
                TheoreticalTime = vertexCount * vertexCount / 10,
                Complexity = "O(V²)"
            });

            points.Add(new PerformancePoint
            {
                VertexCount = vertexCount,
                EdgeCount = edgeCount,
                TheoreticalTime = 1,
                Complexity = "O(1)"
            });
        }

        return new ComplexityReport(points, BuildSummary());
    }

    private static string BuildSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("📊 АНАЛИЗ СЛОЖНОСТИ АЛГОРИТМА ДЕЙКСТРЫ");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("");
        sb.AppendLine("График показывает теоретическую сложность");
        sb.AppendLine("алгоритма Дейкстры в зависимости от размера графа:");
        sb.AppendLine("");
        sb.AppendLine("  🔵 O(1) - константная (идеальный случай)");
        sb.AppendLine("  🟢 O(V log V) - алгоритм с бинарной кучей");
        sb.AppendLine("  🔴 O(V²) - алгоритм без оптимизации");
        sb.AppendLine("");
        sb.AppendLine("Алгоритм Дейкстры с кучей (O(V log V))");
        sb.AppendLine("значительно быстрее на больших графах,");
        sb.AppendLine("чем реализация без кучи (O(V²)).");

        return sb.ToString();
    }
}
