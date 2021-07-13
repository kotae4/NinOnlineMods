using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Pathfinding
{
    public class AStarSearch
    {
        public Dictionary<Vector2i, Vector2i> cameFrom = new Dictionary<Vector2i, Vector2i>();
        public Dictionary<Vector2i, double> costSoFar = new Dictionary<Vector2i, double>();

        // Note: a generic version of A* would abstract over Location and
        // also Heuristic
        static public double Heuristic(Vector2i a, Vector2i b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        public AStarSearch(IWeightedGraph<Vector2i> graph, Vector2i start, Vector2i goal)
        {
            int numTilesSeen = 0;

            var frontier = new PriorityQueue<Vector2i>();
            frontier.Enqueue(start, 0);

            cameFrom[start] = start;
            costSoFar[start] = 0;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (current.Equals(goal))
                {
                    break;
                }

                foreach (var next in graph.Neighbors(current))
                {
                    numTilesSeen++;
                    double newCost = costSoFar[current]
                        + graph.GetCost(current, next);
                    if (!costSoFar.ContainsKey(next)
                        || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        double priority = newCost + Heuristic(next, goal);
                        frontier.Enqueue(next, priority);
                        cameFrom[next] = current;
                    }
                }
            }
            Logger.Log.Write($"Saw {numTilesSeen} valid tiles on pathfinding grid");
        }
    }
}
