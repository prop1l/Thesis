using System;
using System.Collections.Generic;
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

    private static string BuildSummary() =>
        "Реализация с двоичной кучей работает за O((V + E) log V), без кучи — за O(V²). " +
        "На малых графах разница незаметна, но с ростом числа вершин квадратичная " +
        "реализация отстаёт всё сильнее. O(1) показана для сравнения как нижняя граница.";
}
