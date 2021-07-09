using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuikGraph;

namespace NinMods.Bot
{
    public class BotCommand_MoveToMap : IBotCommand
    {
        bool hasFailedCatastrophically = false;
        bool hasReachedDestination = false;

        int TargetMapID = -1;
        Queue<int> interMapPath = new Queue<int>();
        Stack<Vector2i> intraMapPath = new Stack<Vector2i>();
        int lastFrameMapID = -1;
        Vector2i intraMapWarpDirection = new Vector2i(0, 0);

        public BotCommand_MoveToMap(int targetMapID)
        {
            TargetMapID = targetMapID;
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            IEnumerable<Edge<int>> intermapPathEnumerable = null;
            if (InterMapPathfinding.IntermapPathfinding.GetPathFromTo(bot.Map, targetMapID, out intermapPathEnumerable) == false)
            {
                Logger.Log.WriteError("BotCommand_MoveToMap", "ctor", $"Could not find intermap path from {bot.Map} to {targetMapID}");
                hasFailedCatastrophically = true;
                return;
            }
            Logger.Log.Write("BotCommand_MoveToMap", "ctor", $"Got inter-map path to {targetMapID} from {bot.Map}, constructing queue now");
            foreach (Edge<int> edge in intermapPathEnumerable)
            {
                interMapPath.Enqueue(edge.Target);
                Logger.Log.Write("BotCommand_MoveToMap", "ctor", $"Enqueued mapID {edge.Target} (from {edge.Source})");
            }
            Logger.Log.Write("BotCommand_MoveToMap", "ctor", $"Finished constructing intermap queue, there are {interMapPath.Count} maps to traverse. Bot is starting on map {bot.Map}.");
            int nextMapID = interMapPath.Dequeue();
            if (LoadPathToNextMap(nextMapID) == false)
            {
                Logger.Log.WriteError("BotCommand_MoveToMap", "Perform", $"Could not find a path through map '{bot.Map}' to reach map '{nextMapID}'. Cannot continue.");
                hasFailedCatastrophically = true;
                return;
            }
            lastFrameMapID = bot.Map;
            Logger.Log.Write("BotCommand_MoveToMap", "ctor", $"Finished initializing BotCommand_MoveToMap. Pathing to {nextMapID} via {intraMapPath.Count} tiles then facing direction {intraMapWarpDirection}.");
        }

        public bool IsComplete()
        {
            return ((hasReachedDestination == true) && (hasFailedCatastrophically == false));
        }

        public bool Perform()
        {
            if ((interMapPath == null) || (interMapPath.Count == 0))
                hasFailedCatastrophically = true;
            if (hasFailedCatastrophically)
                return false;

            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            Vector2i botLocation = BotUtils.GetSelfLocation();
            if (bot.Map == TargetMapID)
            {
                hasReachedDestination = true;
                return true;
            }
            // WARNING:
            // this is probably not a good idea.
            // we need some way of notifying this command when a map load occurs and use that to determine when to calculate the next intraMapPath.
            if (((intraMapPath.Count == 0) || (lastFrameMapID != bot.Map)) && (client.modGlobals.GettingMap == false))
            {
                // we're either just starting or have just loaded into a new map
                // so we have to find a path from this map to the warp point of the next map
                int nextMapID = interMapPath.Dequeue();
                Logger.Log.Write("BotCommand_MoveToMap", "Perform", $"Saw bot move to new map {bot.Map} from {lastFrameMapID}. Now pathfinding to reach next map {nextMapID}...");
                if (LoadPathToNextMap(nextMapID) == false)
                {
                    Logger.Log.WriteError("BotCommand_MoveToMap", "Perform", $"Could not find a path through map '{bot.Map}' to reach map '{nextMapID}'. Cannot continue.");
                    hasFailedCatastrophically = true;
                    return false;
                }
            }
            if ((intraMapPath.Count != 0) && (BotUtils.CanMove()))
            {
                //Logger.Log.Write("BotCommand_MoveToMap", "Perform", "Got permission to perform movement this tick");
                Vector2i nextTile = intraMapPath.Pop();
                Vector2i tileDirection = nextTile - botLocation;

                if (BotUtils.MoveDir(tileDirection) == false)
                {
                    Logger.Log.WriteError("BotCommand_MoveToMap", "Perform", $"Could not move bot at {botLocation} in direction {tileDirection}");
                    hasFailedCatastrophically = true;
                    return false;
                }
                // if it's the last element, make the bot face the warp direction
                if (intraMapPath.Count == 0)
                {
                    if (BotUtils.FaceDir(intraMapWarpDirection) == false)
                    {
                        Logger.Log.WriteError("BotCommand_MoveToMap", "Perform", $"Could not force bot to face warp direction {intraMapWarpDirection}");
                        hasFailedCatastrophically = true;
                        return false;
                    }
                }
            }
            lastFrameMapID = bot.Map;
            return true;
        }

        bool LoadPathToNextMap(int nextMapID)
        {
            intraMapPath.Clear();
            Vector2i botLocation = BotUtils.GetSelfLocation();
            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Trying to path to next map transition from tile {botLocation}");
            client.modTypes.MapRec currentMap = client.modTypes.Map;
            int tileLengthX = currentMap.Tile.GetLength(0);
            int tileLengthY = currentMap.Tile.GetLength(1);
            Stack<Vector2i> shortestPath = new Stack<Vector2i>();
            Func<int, int, bool> tilePredicate;
            // construct predicate based on where the warp position is within the map
            if (currentMap.Left == nextMapID)
            {
                // find shortest path to X=0,Y=Any
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to left edge of current map, where X=0,Y=Any");
                tilePredicate = (x, y) => { return ((x == 0) && (Pathfinder.IsValidTile(x, y))); };
                intraMapWarpDirection = Vector2i.directions_Eight[(int)Constants.DIR_LEFT];
            }
            else if (currentMap.Up == nextMapID)
            {
                // find shortest path to X=Any,Y=0
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to top edge of current map, where X=Any,Y=0");
                tilePredicate = (x, y) => { return ((y == 0) && (Pathfinder.IsValidTile(x, y))); };
                intraMapWarpDirection = Vector2i.directions_Eight[(int)Constants.DIR_UP];
            }
            else if (currentMap.Right == nextMapID)
            {
                // find shortest path to X=Map.MaxX,Y=Any
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to right edge of current map, where X=MaxX,Y=Any");
                tilePredicate = (x, y) => { return ((x == currentMap.MaxX) && (Pathfinder.IsValidTile(x, y))); };
                intraMapWarpDirection = Vector2i.directions_Eight[(int)Constants.DIR_RIGHT];
            }
            else if (currentMap.Down == nextMapID)
            {
                // find shortest path to X=Any,Y=Map.MaxY
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to down edge of current map, where X=Any,Y=MaxY");
                tilePredicate = (x, y) => { return ((y == currentMap.MaxY) && (Pathfinder.IsValidTile(x, y))); };
                intraMapWarpDirection = Vector2i.directions_Eight[(int)Constants.DIR_DOWN];
            }
            else
            {
                // find shortest path to any valid warp tile
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Map transition isn't on any edge, searching warp points now...");
                tilePredicate = (x, y) => { return ((currentMap.Tile[x, y].Type == (byte)Utilities.GameUtils.ETileType.TILE_TYPE_WARP) 
                    && (currentMap.Tile[x, y].Data1 == nextMapID) 
                    && (Pathfinder.IsValidTile(x, y))); };
                intraMapWarpDirection = Vector2i.zero;
            }
            // now that we have the predicate, get all matching tiles and try pathfinding to each of them
            List<Vector2i> warpTiles = BotUtils.GetAllTilesMatchingPredicate(tilePredicate);
            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Predicate matched {warpTiles.Count} tiles. Pathfinding to each of them now...");
            foreach (Vector2i warpTile in warpTiles)
            {
                shortestPath.Clear();
                shortestPath = Pathfinder.GetPathTo(warpTile.x, warpTile.y, true);
                if ((shortestPath != null) && ((shortestPath.Count < intraMapPath.Count) || (intraMapPath.Count == 0)))
                {
                    // should be a deep-copy. expensive but oh well, this shouldn't run very frequently.
                    Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Got valid path to {warpTile} of length {shortestPath.Count} (prevLength: {intraMapPath.Count})");
                    intraMapPath = new Stack<Vector2i>(shortestPath.ToArray().Reverse());
                }
                else
                {
                    if (shortestPath == null)
                        Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Couldn't path to {warpTile}");
                    if (shortestPath.Count >= intraMapPath.Count)
                        Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Path to {warpTile} was longer than existing path ({shortestPath.Count} vs {intraMapPath.Count})");
                }
            }
            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Found shortest path to reach map {nextMapID} (length: {intraMapPath.Count}, lastTile: {intraMapPath.DefaultIfEmpty(new Vector2i(-1, -1)).LastOrDefault()})");
            return ((intraMapPath != null) && (intraMapPath.Count > 0));
        }
    }
}
