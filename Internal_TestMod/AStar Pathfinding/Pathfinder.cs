using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NinMods.Pathfinding;

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
            // TILE_TYPE_RESOURCE is labeled 'B' by the game, which is the same as TILE_TYPE_BLOCKED, so i'm going to assume it's impassable too
            return !((client.modTypes.Map.Tile[x, y].Type == Constants.TILE_TYPE_BLOCKED) || 
                (client.modTypes.Map.Tile[x, y].Type == Constants.TILE_TYPE_SIT) || 
                (client.modTypes.Map.Tile[x, y].Type == Constants.TILE_TYPE_NPCSPAWN) ||
                (client.modTypes.Map.Tile[x, y].Type == Constants.TILE_TYPE_PLAYERSPAWN) ||
                (client.modTypes.Map.Tile[x, y].Type == Constants.TILE_TYPE_RESOURCE));
        }

        // TO-DO:
        // optimize this. add a GetPathTo_NonAlloc version.
        public static Stack<Vector2i> GetPathTo(int tileX, int tileY)
        {
            Vector2i playerLoc = new Vector2i(client.modTypes.Player[client.modGlobals.MyIndex].X, client.modTypes.Player[client.modGlobals.MyIndex].Y);
            Vector2i targetLoc = new Vector2i(tileX, tileY);
            AStarSearch pathfinder = new AStarSearch(NinMods.Main.MapPathfindingGrid, playerLoc, targetLoc);
            Vector2i step;
            if (!pathfinder.cameFrom.TryGetValue(targetLoc, out step))
            {
                Logger.Log.Write($"Pathfinder could not find path to {targetLoc}");
                return null;
            }
            Logger.Log.Write($"Done pathfinding from {playerLoc} to {targetLoc}, constructing pathStack now");
            Stack<Vector2i> pathStack = new Stack<Vector2i>();
            pathStack.Push(targetLoc);
            while (true)
            {
                if (step == playerLoc)
                {
                    break;
                }
                pathStack.Push(step);
                //Logger.Log.Write("Pathfinder", "GetPathTo", $"Pathfinder stepped from {step}");
                if (!pathfinder.cameFrom.TryGetValue(step, out step))
                {
                    Logger.Log.WriteError($"Pathfinder stopped unexpectedly at {step}");
                    break;
                }
            }
            Logger.Log.Write($"Done constructing path stack from {playerLoc} to {targetLoc}");
            return pathStack;
        }
    }
}