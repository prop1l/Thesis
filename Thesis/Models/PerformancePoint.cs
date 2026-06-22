namespace Thesis.Models
{
    public class PerformancePoint
    {
        public int VertexCount { get; set; }
        public int EdgeCount { get; set; }
        public double TheoreticalTime { get; set; }
        public double ActualTime { get; set; }
        public string Complexity { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
    }
}