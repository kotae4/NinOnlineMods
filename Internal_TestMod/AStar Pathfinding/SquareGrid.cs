using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Pathfinding
{
    public class SquareGrid : IWeightedGraph<Vector2i>
    {
        // Implementation notes: I made the fields public for convenience,
        // but in a real project you'll probably want to follow standard
        // style and make them private.

        private client.modTypes.TileRec[,] m_GridData;

        int minX, minY, maxX, maxY;

        public int Length { get { return m_GridData.Length; } }


        public SquareGrid(client.modTypes.MapRec map, client.modTypes.TileRec[,] gridData)
        {
            minX = map.Left != 0 ? 1 : 0;
            maxX = map.Right != 0 ? map.MaxX - 1 : map.MaxX;

            minY = map.Up != 0 ? 1 : 0;
            maxY = map.Down != 0 ? map.MaxY - 1 : map.MaxY;

            this.m_GridData = gridData;

            Logger.Log.Write($"Initialized pathfinding grid (minXY: {minX}, {minY} maxXY: {maxX}, {maxY}");
        }

        public client.modTypes.TileRec this[int x, int y]
        {
            get { return m_GridData[x, y]; }
            set { m_GridData[x, y] = value; }
        }

        public bool IsInBounds(Vector2i id)
        {
            return id.x >= minX && id.x <= maxX
                && id.y >= minY && id.y <= maxY;
        }

        public bool IsPassable(Vector2i id)
        {
            client.modTypes.TileRec cell = m_GridData[id.x, id.y];

            if ((cell.Type == client.modConstants.TILE_TYPE_BLOCKED)
                || (cell.Type == client.modConstants.TILE_TYPE_SIT)
                || (cell.Type == client.modConstants.TILE_TYPE_NPCSPAWN)
                || (cell.Type == client.modConstants.TILE_TYPE_PLAYERSPAWN)
                || (cell.Type == client.modConstants.TILE_TYPE_RESOURCE)
                || (cell.Type == client.modConstants.TILE_TYPE_WARP))
            {
                //Logger.Log.Write($"Tile {id} is impassable because it's of type {cell.Type} '{(NinMods.Utilities.GameUtils.ETileType)cell.Type}'");
                return false;
            }

            return true;
        }

        public double GetCost(Vector2i from, Vector2i to)
        {
            // NOTE:
            // don't need to check if it's passable; that's already done before reaching this point.
            // TO-DO:
            // add higher cost to tiles surrounding players (so the bot can avoid players [and thus avoid suspicion])
            // possibly add higher cost to water tiles? dunno
            return 1;
        }

        public IEnumerable<Vector2i> Neighbors(Vector2i id)
        {
            foreach (var dir in Vector2i.directions_Eight)
            {
                // TO-DO:
                // if input vector 'id' is a warp event tile (those black rune things on the grind)
                // then return the neighbors of the warp destination tile instead of its own neighbors
                Vector2i next = new Vector2i(id.x + dir.x, id.y + dir.y);
                if (IsInBounds(next))
                {
                    if (IsPassable(next))
                    {
                        yield return next;
                    }
                    else
                    {
                        client.modTypes.TileRec cell = m_GridData[next.x, next.y];
                        //Logger.Log.Write($"Tile {next} is impassable (tileType: {cell.Type} '{(NinMods.Utilities.GameUtils.ETileType)cell.Type}')");
                    }
                }
                else
                {
                    //Logger.Log.Write($"Tile {next} is out of bounds");
                }
            }
        }
    }
}