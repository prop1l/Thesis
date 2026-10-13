using System.Collections.Generic;
using Thesis.Models.Graph;

namespace Thesis.Services;

/// <summary>
/// Хранилище графов. ViewModel-и работают только с этим интерфейсом и не знают,
/// где и в каком формате лежат данные.
/// </summary>
public interface IGraphRepository
{
    IReadOnlyList<GraphItem> GetGraphList();
    void SaveGraphList(IEnumerable<GraphItem> items);

    /// <summary>Загружает граф; если файла нет или он повреждён — возвращает пустой граф.</summary>
    GraphData LoadGraph(string graphName);
    void SaveGraph(string graphName, GraphData graphData);
    void DeleteGraph(string graphName);

    /// <summary>Переносит данные графа под новое имя. Список графов не меняет.</summary>
    void RenameGraph(string oldName, string newName);

    GraphData CreateEmptyGraph(string graphName);
}
