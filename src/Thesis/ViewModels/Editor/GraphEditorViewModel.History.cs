using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Thesis.Models.Graph;

namespace Thesis.ViewModels.Editor;

/// <summary>
/// История изменений графа для Ctrl+Z / Ctrl+Y.
/// Хранятся снимки состояния целиком: граф небольшой, а снимок гарантированно
/// восстанавливает всё — без отдельной «обратной операции» для каждого действия.
/// </summary>
public partial class GraphEditorViewModel
{
    private const int HistoryLimit = 100;

    private readonly List<EditorState> _undoStack = new();
    private readonly List<EditorState> _redoStack = new();

    // Состояние после последнего записанного изменения: при следующем изменении оно уходит в историю
    private EditorState? _currentState;

    // Однотипные изменения подряд (перетаскивание вершины, ввод текста в поле) склеиваются в один шаг
    private string? _lastChangeKey;

    private bool _isRestoringHistory;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_undoStack.Count == 0 || _currentState is null)
            return;

        _redoStack.Add(_currentState);
        RestoreState(Pop(_undoStack));
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_redoStack.Count == 0 || _currentState is null)
            return;

        _undoStack.Add(_currentState);
        RestoreState(Pop(_redoStack));
    }

    /// <summary>Завершает текущую серию склеиваемых изменений (например, отпустили вершину).</summary>
    public void EndChangeGroup()
    {
        _lastChangeKey = null;
    }

    private void ResetHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _lastChangeKey = null;
        _currentState = CaptureState();
        NotifyHistoryState();
    }

    /// <summary>Вызывается после каждого изменения графа.</summary>
    private void RecordChange(string? groupKey = null)
    {
        if (!_isGraphLoaded || _isRestoringHistory || _currentState is null)
            return;

        var newState = CaptureState();
        if (newState.SameAs(_currentState))
            return;

        var continuesGroup = groupKey is not null && groupKey == _lastChangeKey;
        if (!continuesGroup)
        {
            _undoStack.Add(_currentState);
            if (_undoStack.Count > HistoryLimit)
                _undoStack.RemoveAt(0);
        }

        _currentState = newState;
        _lastChangeKey = groupKey;
        _redoStack.Clear();
        NotifyHistoryState();
    }

    private EditorState CaptureState() => new(
        GraphKind,
        Nodes.Select(n => new NodeState(n.Id, n.Label, n.X, n.Y)).ToList(),
        Edges.Select(e => new EdgeState(e.Id, e.SourceId, e.TargetId, e.Label, e.Weight)).ToList());

    private void RestoreState(EditorState state)
    {
        var selectedNodeId = SelectedNode?.Id;
        var selectedEdgeId = SelectedEdge?.Id;
        var startNodeId = SearchStartNode?.Id;
        var endNodeId = SearchEndNode?.Id;

        _isRestoringHistory = true;
        try
        {
            SelectedNode = null;
            SelectedEdge = null;
            CancelEdgeCreation();
            ClearSearch();
            ClearGraph();

            // Тип меняем до добавления рёбер, чтобы OnGraphKindChanged не переписал их веса
            GraphKind = state.Kind;

            foreach (var n in state.Nodes)
                Nodes.Add(new NodeViewModel(new Node { Id = n.Id, Label = n.Label, X = n.X, Y = n.Y }, GraphStyle.Node));

            var nodesById = Nodes.ToDictionary(n => n.Id);

            foreach (var e in state.Edges)
            {
                var edge = new Edge { Id = e.Id, SourceId = e.SourceId, TargetId = e.TargetId, Label = e.Label, Weight = e.Weight };
                Edges.Add(new EdgeViewModel(edge, nodesById[e.SourceId], nodesById[e.TargetId], GraphStyle));
            }

            RebuildLogicalWeights();

            SelectedNode = Nodes.FirstOrDefault(n => n.Id == selectedNodeId);
            SelectedEdge ??= Edges.FirstOrDefault(e => e.Id == selectedEdgeId);
            SearchStartNode = Nodes.FirstOrDefault(n => n.Id == startNodeId);
            SearchEndNode = Nodes.FirstOrDefault(n => n.Id == endNodeId);
        }
        finally
        {
            _isRestoringHistory = false;
        }

        _currentState = state;
        _lastChangeKey = null;

        NotifyState();
        NotifyHistoryState();
        SaveGraph();
        UpdateAdjacencyMatrix();
    }

    private void NotifyHistoryState()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private static EditorState Pop(List<EditorState> stack)
    {
        var state = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return state;
    }

    private sealed record NodeState(string Id, string Label, double X, double Y);

    private sealed record EdgeState(string Id, string SourceId, string TargetId, string Label, decimal? Weight);

    private sealed record EditorState(GraphKind Kind, List<NodeState> Nodes, List<EdgeState> Edges)
    {
        public bool SameAs(EditorState other) =>
            Kind == other.Kind && Nodes.SequenceEqual(other.Nodes) && Edges.SequenceEqual(other.Edges);
    }
}
