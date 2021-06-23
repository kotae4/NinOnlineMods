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

        public static readonly Vector2i[] DIRS = new[]
            {
            new Vector2i(1, 0),
            new Vector2i(0, -1),
            new Vector2i(-1, 0),
            new Vector2i(0, 1)
        };

        private client.modTypes.TileRec[,] m_GridData;

        public int width, height;

        public int Length { get { return m_GridData.Length; } }


        public SquareGrid(client.modTypes.TileRec[,] gridData, int width, int height)
        {
            this.m_GridData = gridData;
            this.width = width;
            this.height = height;
        }

        public client.modTypes.TileRec this[int x, int y]
        {
            get { return m_GridData[x, y]; }
            set { m_GridData[x, y] = value; }
        }

        public bool IsInBounds(Vector2i id)
        {
            return 0 <= id.x && id.x < width
                && 0 <= id.y && id.y < height;
        }

        public bool IsPassable(Vector2i id)
        {
            client.modTypes.TileRec cell = m_GridData[id.x, id.y];

            return !((cell.Type == Constants.TILE_TYPE_BLOCKED) ||
                (cell.Type == Constants.TILE_TYPE_SIT) ||
                (cell.Type == Constants.TILE_TYPE_NPCSPAWN) ||
                (cell.Type == Constants.TILE_TYPE_PLAYERSPAWN) ||
                (cell.Type == Constants.TILE_TYPE_RESOURCE));
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
            foreach (var dir in DIRS)
            {
                Vector2i next = new Vector2i(id.x + dir.x, id.y + dir.y);
                if (IsInBounds(next) && IsPassable(next))
                {
                    yield return next;
                }
            }
        }
    }
}