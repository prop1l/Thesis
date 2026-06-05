using CommunityToolkit.Mvvm.ComponentModel;

namespace Thesis.Models;

public partial class GraphStyle : ObservableObject
{
    [ObservableProperty] private NodeStyle node = new();
    [ObservableProperty] private EdgeStyle edge = new();
}