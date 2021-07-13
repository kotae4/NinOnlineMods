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
        int nextMapID = -1;
        int currentMapID = -1;
        Vector2i intraMapWarpTile = new Vector2i(-2, -2);
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
            nextMapID = interMapPath.Dequeue();
            if (LoadPathToNextMap(nextMapID) == false)
            {
                Logger.Log.WriteError("BotCommand_MoveToMap", "Perform", $"Could not find a path through map '{bot.Map}' to reach map '{nextMapID}' from botLocation ({bot.X} {bot.Y}). Cannot continue.");
                hasFailedCatastrophically = true;
                return;
            }
            currentMapID = bot.Map;
            Logger.Log.Write("BotCommand_MoveToMap", "ctor", $"Finished initializing BotCommand_MoveToMap. Pathing to {nextMapID} via {intraMapPath.Count} tiles then facing direction {intraMapWarpDirection}.");
        }

        public bool IsComplete()
        {
            return ((hasReachedDestination == true) && (hasFailedCatastrophically == false));
        }

        public bool Perform()
        {
            if (interMapPath == null)
            {
                Logger.Log.WriteError("BotCommand_MoveToMap", "Perform", $"Intermap Path was null, cannot continue");
                hasFailedCatastrophically = true;
            }
            if (hasFailedCatastrophically)
                return false;

            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            Vector2i botLocation = BotUtils.GetSelfLocation();
            if (bot.Map == TargetMapID)
            {
                Logger.Log.WriteError("BotCommand_MoveToMap", "Perform", $"Bot has reached final map {bot.Map} == {TargetMapID}");
                hasReachedDestination = true;
                return true;
            }
            // WARNING:
            // this is probably not a good idea.
            // we need some way of notifying this command when a map load occurs and use that to determine when to calculate the next intraMapPath.
            double distanceToWarp = botLocation.DistanceTo(intraMapWarpTile);
            if (((intraMapPath.Count == 0) || (currentMapID != bot.Map)) && (client.modGlobals.GettingMap == false) && (distanceToWarp > 2.0d))
            {
                Logger.Log.Write("BotCommand_MoveToMap", "Perform", $"Recalculating intraMapPath (bot: {botLocation}, warpTile: {intraMapWarpTile}, gettingMap: {client.modGlobals.GettingMap}, or-expr: {((intraMapPath.Count == 0) || (currentMapID != bot.Map))}");
                // we're either just starting or have just loaded into a new map
                // so we have to find a path from this map to the warp point of the next map
                nextMapID = interMapPath.Dequeue();
                Logger.Log.Write("BotCommand_MoveToMap", "Perform", $"Saw bot move to new map {bot.Map} from {currentMapID}. Now pathfinding to reach next map {nextMapID}...");
                currentMapID = bot.Map;
                if (LoadPathToNextMap(nextMapID) == false)
                {
                    Logger.Log.WriteError("BotCommand_MoveToMap", "Perform", $"Could not find a path through map '{bot.Map}' to reach map '{nextMapID}'. Cannot continue.");
                    hasFailedCatastrophically = true;
                    return false;
                }
            }
            if (BotUtils.CanMove())
            {
                if (intraMapPath.Count > 0)
                {
                    //Logger.Log.Write("BotCommand_MoveToMap", "Perform", "Got permission to perform movement this tick");
                    Vector2i nextTile = intraMapPath.Pop();
                    Vector2i tileDirection = nextTile - botLocation;
                    Logger.Log.WriteError("BotCommand_MoveToMap", "Perform", $"Moving bot toward map transition (next: {nextTile}, dir: {tileDirection}, warpPos: {intraMapWarpTile}, warpDir: {intraMapWarpDirection})");
                    if (BotUtils.MoveDir(tileDirection) == false)
                    {
                        Logger.Log.WriteError("BotCommand_MoveToMap", "Perform", $"Could not move bot at {botLocation} in direction {tileDirection}");
                        hasFailedCatastrophically = true;
                        return false;
                    }
                }
                // if it's the last element, make the bot face the warp direction
                else
                {
                    // TO-DO:
                    // think about this more. what if there's a boulder at the veeery edge of the map? our bot would get stuck running into it.
                    // on the other hand, i doubt there'll be a boulder at the edge of the map.
                    if (BotUtils.MoveDir(intraMapWarpDirection) == false)
                    {
                        Logger.Log.WriteError("BotCommand_MoveToMap", "Perform", $"Could not move bot at {botLocation} toward final warp tile in direction {intraMapWarpDirection}");
                        hasFailedCatastrophically = true;
                        return false;
                    }
                }
            }
            return true;
        }

        bool LoadPathToNextMap(int nextMapID)
        {
            intraMapPath.Clear();
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            Vector2i botLocation = BotUtils.GetSelfLocation();
            // too lazy to sync up currentMapID class field
            // TO-DO:
            // don't be lazy
            int actualCurrentMapID = bot.Map;
            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Trying to path to next map transition from tile {botLocation}");
            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"MyIndex: {client.modGlobals.MyIndex}, bot: {client.modTypes.Player[client.modGlobals.MyIndex]}," +
                $"GettingMap: {client.modGlobals.GettingMap}");
            client.modTypes.MapRec currentMap = client.modTypes.Map;
            int tileLengthX = currentMap.Tile.GetLength(0);
            int tileLengthY = currentMap.Tile.GetLength(1);
            Stack<Vector2i> shortestPath = new Stack<Vector2i>();
            Func<Vector2i, bool> tilePredicate;
            // construct predicate based on where the warp position is within the map
            if (currentMap.Left == nextMapID)
            {
                // find shortest path to X=0,Y=Any
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to left edge of current map, where X=0,Y=Any");
                tilePredicate = (tilePos) => { return ((tilePos.x == 1) && (Pathfinder.IsValidTile(tilePos.x, tilePos.y)) && (MapData.IsBlacklistedTile(actualCurrentMapID, tilePos) == false)); };
                intraMapWarpDirection = Vector2i.directions_Eight[(int)Constants.DIR_LEFT];
            }
            else if (currentMap.Up == nextMapID)
            {
                // find shortest path to X=Any,Y=0
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to top edge of current map, where X=Any,Y=0");
                tilePredicate = (tilePos) => { return ((tilePos.y == 1) && (Pathfinder.IsValidTile(tilePos.x, tilePos.y)) && (MapData.IsBlacklistedTile(actualCurrentMapID, tilePos) == false)); };
                intraMapWarpDirection = Vector2i.directions_Eight[(int)Constants.DIR_UP];
            }
            else if (currentMap.Right == nextMapID)
            {
                // find shortest path to X=Map.MaxX,Y=Any
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to right edge of current map, where X=MaxX,Y=Any");
                tilePredicate = (tilePos) => { return ((tilePos.x == currentMap.MaxX - 1) && (Pathfinder.IsValidTile(tilePos.x, tilePos.y)) && (MapData.IsBlacklistedTile(actualCurrentMapID, tilePos) == false)); };
                intraMapWarpDirection = Vector2i.directions_Eight[(int)Constants.DIR_RIGHT];
            }
            else if (currentMap.Down == nextMapID)
            {
                // find shortest path to X=Any,Y=Map.MaxY
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to down edge of current map, where X=Any,Y=MaxY");
                tilePredicate = (tilePos) => { return ((tilePos.y == currentMap.MaxY - 1) && (Pathfinder.IsValidTile(tilePos.x, tilePos.y)) && (MapData.IsBlacklistedTile(actualCurrentMapID, tilePos) == false)); };
                intraMapWarpDirection = Vector2i.directions_Eight[(int)Constants.DIR_DOWN];
            }
            else
            {
                // find shortest path to any valid warp tile
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Map transition isn't on any edge, searching warp points now...");
                tilePredicate = (tilePos) => {
                    return ((currentMap.Tile[tilePos.x, tilePos.y].Type == (byte)Utilities.GameUtils.ETileType.TILE_TYPE_WARP) 
                    && (currentMap.Tile[tilePos.x, tilePos.y].Data1 == nextMapID)
                    && (Pathfinder.IsValidTile(tilePos.x, tilePos.y))
                    && (MapData.IsBlacklistedTile(actualCurrentMapID, tilePos) == false)); };
                intraMapWarpDirection = Vector2i.zero;
            }
            // now that we have the predicate, get all matching tiles and try pathfinding to each of them
            List<Vector2i> warpTiles = BotUtils.GetAllTilesMatchingPredicate(tilePredicate);
            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Predicate matched {warpTiles.Count} tiles. Pathfinding to each of them now...");
            foreach (Vector2i warpTile in warpTiles)
            {
                if (shortestPath != null)
                    shortestPath.Clear();
                shortestPath = Pathfinder.GetPathTo(warpTile.x, warpTile.y);
                if ((shortestPath != null) && ((shortestPath.Count < intraMapPath.Count) || (intraMapPath.Count == 0)))
                {
                    // should be a deep-copy. expensive but oh well, this shouldn't run very frequently.
                    Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Got valid path to {warpTile} of length {shortestPath.Count} (prevLength: {intraMapPath.Count})");
                    intraMapPath = new Stack<Vector2i>(shortestPath.ToArray().Reverse());
                    intraMapWarpTile = warpTile + intraMapWarpDirection;
                }
                else
                {
                    if (shortestPath == null)
                        Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Couldn't path to {warpTile}");
                    if ((shortestPath != null) && (shortestPath.Count >= intraMapPath.Count))
                        Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Path to {warpTile} was longer than existing path ({shortestPath.Count} vs {intraMapPath.Count})");
                }
            }
            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Found shortest path to reach map {nextMapID} (length: {intraMapPath.Count}, lastTile: {intraMapPath.DefaultIfEmpty(new Vector2i(-1, -1)).LastOrDefault()})");
            return ((intraMapPath != null) && (intraMapPath.Count > 0));
        }
    }
}
