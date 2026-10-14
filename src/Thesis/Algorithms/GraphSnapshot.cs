namespace Thesis.Algorithms;

// Лёгкие неизменяемые представления графа для алгоритмов.
// Алгоритмы получают «снимок» графа и не зависят от ViewModel-ей и UI.

/// <summary>Вершина; координаты центра нужны эвристическим алгоритмам (A*, жадный поиск).</summary>
public sealed record GraphVertex(string Id, string Label, double X = 0, double Y = 0);

public sealed record GraphArc(string Id, string SourceId, string TargetId, string Label, decimal? Weight);
