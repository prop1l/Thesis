using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Thesis.Models;

public partial class Node : ObservableObject
{
    [ObservableProperty] private string id = Guid.NewGuid().ToString();
    [ObservableProperty] private string label = string.Empty;
    [ObservableProperty] private double x = 100;
    [ObservableProperty] private double y = 100;

    [ObservableProperty] private bool isVisited;
    [ObservableProperty] private bool isHighlighted;
    [ObservableProperty] private string distanceLabel = string.Empty;
}