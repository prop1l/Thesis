using Avalonia;
using System.Collections.Generic;
using System.Linq;
using Thesis.ViewModels;

namespace Thesis.Helpers;

public static class GraphHitTestHelper
{
    public static NodeViewModel? HitTestNode(IEnumerable<NodeViewModel> nodes, Point p)
    {
        return nodes.FirstOrDefault(n => n.Contains(p));
    }

    public static EdgeViewModel? HitTestEdge(IEnumerable<EdgeViewModel> edges, Point p, double threshold = 8.0)
    {
        EdgeViewModel? nearestEdge = null;
        double minDistance = double.MaxValue;

        foreach (var edge in edges)
        {
            var distance = GraphGeometryHelper.DistanceToSegment(p, edge.StartPoint, edge.EndPoint);

            if (distance <= threshold && distance < minDistance)
            {
                minDistance = distance;
                nearestEdge = edge;
            }
        }

        return nearestEdge;
    }
}
