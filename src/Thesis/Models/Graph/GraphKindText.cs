namespace Thesis.Models.Graph;

public static class GraphKindText
{
    public static string ToTitle(this GraphKind kind) => kind switch
    {
        GraphKind.UndirectedUnweighted => "Неориентированный невзвешенный",
        GraphKind.UndirectedWeighted => "Неориентированный взвешенный",
        GraphKind.DirectedUnweighted => "Ориентированный невзвешенный",
        GraphKind.DirectedWeighted => "Ориентированный взвешенный",
        _ => kind.ToString()
    };

    /// <summary>Склонение по числу: 1 вершина, 2 вершины, 5 вершин.</summary>
    public static string Plural(int count, string one, string few, string many)
    {
        var n = count % 100;
        if (n is >= 11 and <= 14)
            return many;

        return (n % 10) switch
        {
            1 => one,
            2 or 3 or 4 => few,
            _ => many
        };
    }
}
