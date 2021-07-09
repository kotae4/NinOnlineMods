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
            Logger.Log.Write("BotCommand_MoveToMap", "ctor", $"Finished initializing BotCommand_MoveToMap, there are {interMapPath.Count} maps to traverse. Bot is starting on map {bot.Map}.");
            lastFrameMapID = bot.Map;
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
            if ((intraMapPath.Count == 0) || (lastFrameMapID != bot.Map))
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
            // my brain is fried. there has to be a more elegant way of writing this.
            // TO-DO:
            // revisit this later when brain worky good
            Stack<Vector2i> shortestPath = new Stack<Vector2i>();
            if (currentMap.Left == nextMapID)
            {
                // find shortest path to X=0,Y=Any
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to left edge of current map, where X=0,Y=Any");
                for (int tileY = 0; tileY < tileLengthY; tileY++)
                {
                    shortestPath.Clear();
                    if (Pathfinder.IsValidTile(0, tileY) == false)
                        continue;
                    // TO-DO:
                    // optimize this. add a GetPathTo_NonAlloc version.
                    shortestPath = Pathfinder.GetPathTo(-1, tileY, true);
                    if ((shortestPath != null) && ((shortestPath.Count < intraMapPath.Count) || (intraMapPath.Count == 0)))
                    {
                        // should be a deep-copy. expensive but oh well, this shouldn't run very frequently.
                        Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Got valid path to ({-1}, {tileY}) of length {shortestPath.Count} (prevLength: {intraMapPath.Count})");
                        intraMapPath = new Stack<Vector2i>(shortestPath.ToArray().Reverse());
                    }
                    else
                    {
                        if (shortestPath == null)
                            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Couldn't path to ({-1}, {tileY})");
                        if (shortestPath.Count >= intraMapPath.Count)
                            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Path to ({-1}, {tileY}) was longer than existing path ({shortestPath.Count} vs {intraMapPath.Count})");
                    }
                }
            }
            else if (currentMap.Up == nextMapID)
            {
                // find shortest path to X=Any,Y=0
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to top edge of current map, where X=Any,Y=0");
                for (int tileX = 0; tileX < tileLengthX; tileX++)
                {
                    shortestPath.Clear();
                    if (Pathfinder.IsValidTile(tileX, 0) == false)
                        continue;
                    // TO-DO:
                    // optimize this. add a GetPathTo_NonAlloc version.
                    shortestPath = Pathfinder.GetPathTo(tileX, -1, true);
                    if ((shortestPath != null) && ((shortestPath.Count < intraMapPath.Count) || (intraMapPath.Count == 0)))
                    {
                        // should be a deep-copy. expensive but oh well, this shouldn't run very frequently.
                        Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Got valid path to ({tileX}, {-1}) of length {shortestPath.Count} (prevLength: {intraMapPath.Count})");
                        intraMapPath = new Stack<Vector2i>(shortestPath.ToArray().Reverse());
                    }
                    else
                    {
                        if (shortestPath == null)
                            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Couldn't path to ({tileX}, {-1})");
                        if (shortestPath.Count >= intraMapPath.Count)
                            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Path to ({tileX}, {-1}) was longer than existing path ({shortestPath.Count} vs {intraMapPath.Count})");
                    }
                }
            }
            else if (currentMap.Right == nextMapID)
            {
                // find shortest path to X=Map.MaxX,Y=Any
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to right edge of current map, where X=MaxX,Y=Any");
                for (int tileY = 0; tileY < tileLengthY; tileY++)
                {
                    shortestPath.Clear();
                    if (Pathfinder.IsValidTile(currentMap.MaxX, tileY) == false)
                        continue;
                    // TO-DO:
                    // optimize this. add a GetPathTo_NonAlloc version.
                    shortestPath = Pathfinder.GetPathTo(currentMap.MaxX + 1, tileY, true);
                    if ((shortestPath != null) && ((shortestPath.Count < intraMapPath.Count) || (intraMapPath.Count == 0)))
                    {
                        // should be a deep-copy. expensive but oh well, this shouldn't run very frequently.
                        Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Got valid path to ({currentMap.MaxX + 1}, {tileY}) of length {shortestPath.Count} (prevLength: {intraMapPath.Count})");
                        intraMapPath = new Stack<Vector2i>(shortestPath.ToArray().Reverse());
                    }
                    else
                    {
                        if (shortestPath == null)
                            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Couldn't path to ({currentMap.MaxX + 1}, {tileY})");
                        if (shortestPath.Count >= intraMapPath.Count)
                            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Path to ({currentMap.MaxX + 1}, {tileY}) was longer than existing path ({shortestPath.Count} vs {intraMapPath.Count})");
                    }
                }
            }
            else if (currentMap.Down == nextMapID)
            {
                // find shortest path to X=Any,Y=Map.MaxY
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Pathing to down edge of current map, where X=Any,Y=MaxY");
                for (int tileX = 0; tileX < tileLengthX; tileX++)
                {
                    shortestPath.Clear();
                    if (Pathfinder.IsValidTile(tileX, currentMap.MaxY) == false)
                        continue;
                    // TO-DO:
                    // optimize this. add a GetPathTo_NonAlloc version.
                    shortestPath = Pathfinder.GetPathTo(tileX, currentMap.MaxY + 1, true);
                    if ((shortestPath != null) && ((shortestPath.Count < intraMapPath.Count) || (intraMapPath.Count == 0)))
                    {
                        // should be a deep-copy. expensive but oh well, this shouldn't run very frequently.
                        Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Got valid path to ({tileX}, {currentMap.MaxY + 1}) of length {shortestPath.Count} (prevLength: {intraMapPath.Count})");
                        intraMapPath = new Stack<Vector2i>(shortestPath.ToArray().Reverse());
                    }
                    else
                    {
                        if (shortestPath == null)
                            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Couldn't path to ({tileX}, {currentMap.MaxY + 1})");
                        if (shortestPath.Count >= intraMapPath.Count)
                            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Path to ({tileX}, {currentMap.MaxY + 1}) was longer than existing path ({shortestPath.Count} vs {intraMapPath.Count})");
                    }
                }
            }
            else
            {
                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", "Map transition isn't on any edge, searching warp points now...");
                for (int tileX = 0; tileX < tileLengthX; tileX++)
                {
                    for (int tileY = 0; tileY < tileLengthY; tileY++)
                    {
                        client.modTypes.TileRec tile = currentMap.Tile[tileX, tileY];
                        NinMods.Utilities.GameUtils.ETileType tileType = (NinMods.Utilities.GameUtils.ETileType)tile.Type;
                        if ((tileType == Utilities.GameUtils.ETileType.TILE_TYPE_WARP) && (tile.Data1 == nextMapID))
                        {
                            // path to this exact tile
                            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Saw valid warp point at ({tileX}, {tileY}), trying to path to it now");
                            shortestPath = Pathfinder.GetPathTo(tileX, tileY, true);
                            if ((shortestPath != null) && ((shortestPath.Count < intraMapPath.Count) || (intraMapPath.Count == 0)))
                            {
                                // should be a deep-copy. expensive but oh well, this shouldn't run very frequently.
                                Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Got valid path to ({tileX}, {tileY}) of length {shortestPath.Count} (prevLength: {intraMapPath.Count})");
                                intraMapPath = new Stack<Vector2i>(shortestPath.ToArray().Reverse());
                            }
                            else
                            {
                                if (shortestPath == null)
                                    Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Couldn't path to ({tileX}, {tileY})");
                                if (shortestPath.Count >= intraMapPath.Count)
                                    Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Path to ({tileX}, {tileY}) was longer than existing path ({shortestPath.Count} vs {intraMapPath.Count})");
                            }
                        }
                    }
                }
            }
            Logger.Log.Write("BotCommand_MoveToMap", "LoadPathToNextMap", $"Found shortest path to reach map {nextMapID} (length: {intraMapPath.Count}, lastTile: {intraMapPath.DefaultIfEmpty(new Vector2i(-1, -1)).LastOrDefault()})");
            return ((intraMapPath != null) && (intraMapPath.Count > 0));
        }
    }
}
