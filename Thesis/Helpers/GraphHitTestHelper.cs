using Avalonia;
using System.Collections.Generic;
using System.Linq;
using Thesis.Models;
using Thesis.ViewModels;

namespace Thesis.Helpers;

public static class GraphHitTestHelper
{
    public static Node? HitTestNode(IEnumerable<Node> nodes, Point p, double width, double height)
    {
        return nodes.FirstOrDefault(n =>
            p.X >= n.X && p.X <= n.X + width &&
            p.Y >= n.Y && p.Y <= n.Y + height);
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