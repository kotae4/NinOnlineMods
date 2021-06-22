using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NinMods.Pathfinding
{
    // A* needs only a WeightedGraph and a location type L, and does *not*
    // have to be a grid. However, in the example code I am using a grid.
    public interface IWeightedGraph<L>
    {
        double GetCost(Location a, Location b);
        IEnumerable<Location> Neighbors(Location id);
    }
}
