using CommunityToolkit.Mvvm.ComponentModel;

namespace Thesis.Models;

public partial class NodeStyle : ObservableObject
{
    [ObservableProperty] private double width = 40;
    [ObservableProperty] private double height = 40;
    [ObservableProperty] private string fill = "4CAF50";
    [ObservableProperty] private string stroke = "2E7D32";
    [ObservableProperty] private double strokeThickness = 2;
    [ObservableProperty] private string foreground = "FFFFFF";
    [ObservableProperty] private double fontSize = 13;
}