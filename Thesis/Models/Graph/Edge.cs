using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Thesis.Models;

public partial class Edge : ObservableObject
{
    [ObservableProperty] private string id = Guid.NewGuid().ToString();
    [ObservableProperty] private string sourceId = string.Empty;
    [ObservableProperty] private string targetId = string.Empty;
    [ObservableProperty] private string label = string.Empty;
    [ObservableProperty] private decimal? weight = 1;
}