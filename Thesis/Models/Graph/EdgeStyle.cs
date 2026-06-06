using CommunityToolkit.Mvvm.ComponentModel;

namespace Thesis.Models;

public partial class EdgeStyle : ObservableObject
{
    [ObservableProperty] private string stroke = "696969";
    [ObservableProperty] private double strokeThickness = 2;
    [ObservableProperty] private string foreground = "222222";
    [ObservableProperty] private double fontSize = 12;
    [ObservableProperty] private string weightBackground = "000000";
    [ObservableProperty] private bool isDashed;
}