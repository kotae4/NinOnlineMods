using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods
{
    public static class Pathfinder
    {
        public static bool IsValidTile(int x, int y)
        {
            // NOTES:
            // TILE_TYPE_NPCSPAWN seems to be the pillars at village exit, and they seem to be 2 tiles wide even though the label is only for 1 tile??
            //      * i might be wrong about this. it might just be 1 tile, i dunno.
            // NPCs themselves also block movement, and obviously don't have tile data (so we need to loop through tiles AND MapNpcs)
            // TILE_TYPE_SIT is (can be) blocking for like, a quarter of the tile. probably best to avoid it altogether.
            // TILE_TYPE_PLAYERSPAWN (can? always?) forces the character onto the tile and into an animation that requires input to break
            // TILE_TYPE_THROUGH is passable, and might be related to NPC pathing or something? seems to be only used for interior buildings / portal to interior
            // TILE_TYPE_WARP isn't reliable at all. some warp points don't have any tile type, or have some other type.
            // TILE_TYPE_WATER is passable, but looks like it modifies movement in some way
            return !((client.modTypes.Map.Tile[x, y].Type == Constants.TILE_TYPE_BLOCKED) || 
                (client.modTypes.Map.Tile[x, y].Type == Constants.TILE_TYPE_SIT) || 
                (client.modTypes.Map.Tile[x, y].Type == Constants.TILE_TYPE_NPCSPAWN) ||
                (client.modTypes.Map.Tile[x, y].Type == Constants.TILE_TYPE_PLAYERSPAWN));
        }

        public static Queue<SFML.System.Vector2i> GetPathTo(int tileX, int tileY)
        {
            Queue<SFML.System.Vector2i> optimalPath = new Queue<SFML.System.Vector2i>();
            // TO-DO:
            // pathfinding algo (can put it in another function, just populate optimalPath with result)
            return optimalPath;
        }
    }
}