namespace Thesis.Algorithms;

// Лёгкие неизменяемые представления графа для алгоритмов.
// Алгоритмы получают «снимок» графа и не зависят от ViewModel-ей и UI.

public sealed record GraphVertex(string Id, string Label);

public sealed record GraphArc(string Id, string SourceId, string TargetId, string Label, decimal? Weight);
