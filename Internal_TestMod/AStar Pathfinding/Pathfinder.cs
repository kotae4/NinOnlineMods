﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NinMods.Pathfinding;
using NinMods.Logging;

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
        public static Stack<Vector2i> GetPathTo(int tileX, int tileY, bool allowAdjacentTilesIfNoPathFound = false)
        {
            Vector2i playerLoc = new Vector2i(client.modTypes.Player[client.modGlobals.MyIndex].X, client.modTypes.Player[client.modGlobals.MyIndex].Y);
            Vector2i targetLoc = new Vector2i(tileX, tileY);
            Vector2i adjacentLoc = new Vector2i(0, 0);
            
            AStarSearch pathfinder = new AStarSearch(NinMods.Main.MapPathfindingGrid, playerLoc, targetLoc);
            Vector2i step;
            bool hasExactPath = true;
            bool hasAdjacentPath = false;
            if (!pathfinder.cameFrom.TryGetValue(targetLoc, out step))
            {
                hasExactPath = false;
            }
            if (hasExactPath == false)
            {
                foreach(Vector2i dir in Vector2i.directions_Eight)
                {
                    adjacentLoc = targetLoc + dir;
                    if (pathfinder.cameFrom.TryGetValue(adjacentLoc, out step))
                    {
                        hasAdjacentPath = true;
                        Logger.Log.Write($"Pathfinder found path to an adjacent tile {adjacentLoc} from {playerLoc}");
                        break;
                    }
                }
                if (hasAdjacentPath == false)
                {
                    Logger.Log.Write($"Pathfinder could not find path to {targetLoc} or any adjacent tiles from {playerLoc}");
                    return null;
                }
            }
            Stack<Vector2i> pathStack = new Stack<Vector2i>();
            pathStack.Push((hasExactPath ? targetLoc : adjacentLoc));
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
                    Logger.Log.WriteError($"Pathfinder stopped unexpectedly at {step} when trying to path from {targetLoc} to {playerLoc}");
                    break;
                }
            }
            if (hasAdjacentPath)
                Logger.Log.Write($"Returning path from {playerLoc} to {targetLoc} via adjacentTile {adjacentLoc}");
            else
                Logger.Log.Write($"Returning path from {playerLoc} to {targetLoc}");
            return pathStack;
        }
    }
}