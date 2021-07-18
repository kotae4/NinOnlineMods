using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuikGraph;
using NinMods.Logging;

namespace NinMods.Bot
{
    public class BotCommand_MoveToMap : IBotCommand
    {
        bool hasFailedCatastrophically = false;
        bool hasReachedDestination = false;

        int TargetMapID = -1;
        Queue<int> interMapPath = new Queue<int>();

        int nextMapID = -1;
        int currentMapID = -1;
        bool isAtWarpTile = false;
        bool isMovingByPath = false;
        List<Vector2i> warpTilesToNextMap = new List<Vector2i>();
        Stack<Vector2i> intraMapPath = new Stack<Vector2i>();
        Vector2i intraMapWarpTile = new Vector2i(-2, -2);
        Vector2i intraMapWarpDirection = new Vector2i(0, 0);

        public BotCommand_MoveToMap(int targetMapID)
        {
            TargetMapID = targetMapID;
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            Vector2i botLocation = BotUtils.GetSelfLocation();
            IEnumerable<Edge<int>> intermapPathEnumerable = null;
            if (InterMapPathfinding.IntermapPathfinding.GetPathFromTo(bot.Map, targetMapID, out intermapPathEnumerable) == false)
            {
                Logger.Log.WriteError($"Could not find intermap path from {bot.Map} to {targetMapID}");
                hasFailedCatastrophically = true;
                return;
            }
            Logger.Log.Write($"Got inter-map path to {targetMapID} from {bot.Map}, constructing queue now");
            foreach (Edge<int> edge in intermapPathEnumerable)
            {
                interMapPath.Enqueue(edge.Target);
                Logger.Log.Write($"Enqueued mapID {edge.Target} (from {edge.Source})");
            }
            Logger.Log.Write($"Finished constructing intermap queue, there are {interMapPath.Count} maps to traverse. Bot is starting on map {bot.Map}.");
            if (InitializeForMap() == false)
            {
                Logger.Log.WriteError($"Could not initialize for map {bot.Map}, nextMap: {nextMapID}, botLocation: {botLocation}");
                hasFailedCatastrophically = true;
                return;
            }
            Logger.Log.Write($"Finished initializing BotCommand_MoveToMap. Pathing to {nextMapID} via {intraMapPath.Count} tiles then facing direction {intraMapWarpDirection}.");
        }

        public bool IsComplete()
        {
            return ((hasReachedDestination == true) && (hasFailedCatastrophically == false));
        }

        public bool Perform()
        {
            if (interMapPath == null)
            {
                Logger.Log.WriteError($"Intermap Path was null, cannot continue");
                hasFailedCatastrophically = true;
            }
            if (hasFailedCatastrophically)
                return false;

            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            Vector2i botLocation = BotUtils.GetSelfLocation();
            if (bot.Map == TargetMapID)
            {
                Logger.Log.WriteError($"Bot has reached final map {bot.Map} == {TargetMapID}");
                hasReachedDestination = true;
                return true;
            }
            // WARNING:
            // this is probably not a good idea.
            // we need some way of notifying this command when a map load occurs and use that to determine when to calculate the next intraMapPath.
            if ((currentMapID != bot.Map) && (client.modGlobals.GettingMap == false))
            {
                if (InitializeForMap() == false)
                {
                    Logger.Log.WriteError($"Could not re-initialize for new map {bot.Map} (oldMap: {currentMapID}, nextMap: {nextMapID}, botLocation: {botLocation})");
                    hasFailedCatastrophically = true;
                    return false;
                }
            }
            if (BotUtils.CanMove())
            {
                if ((isMovingByPath == true) && (intraMapPath.Count > 0))
                {
                    //Logger.Log.Write("BotCommand_MoveToMap", "Perform", "Got permission to perform movement this tick");
                    Vector2i nextTile = intraMapPath.Pop();
                    Vector2i tileDirection = nextTile - botLocation;
                    Logger.Log.Write($"Moving bot toward map transition (botPos: {botLocation}, next: {nextTile}, dir: {tileDirection}, warpPos: {intraMapWarpTile}, warpDir: {intraMapWarpDirection})");
                    if (BotUtils.MoveDir(tileDirection) == false)
                    {
                        Logger.Log.WriteError($"Could not move bot at {botLocation} in direction {tileDirection}");
                        hasFailedCatastrophically = true;
                        return false;
                    }
                }
                // if it's the last element, make the bot face the warp direction
                else if ((isAtWarpTile == true) || ((isMovingByPath == true) && (intraMapPath.Count == 0)))
                {
                    // WARNING:
                    // there are boulders (or, rather, just blocked tiles) at the edge of map 187 (Valley of the End)...
                    // need to think about how to solve this
                    if (intraMapWarpDirection == Vector2i.zero)
                        intraMapWarpDirection = intraMapWarpTile - botLocation;
                    Logger.Log.Write($"Moving bot toward final map transition (botPos: {botLocation}, warpPos: {intraMapWarpTile}, warpDir: {intraMapWarpDirection})");
                    if (BotUtils.MoveDir(intraMapWarpDirection) == false)
                    {
                        Logger.Log.WriteError($"Could not move bot at {botLocation} toward final warp tile in direction {intraMapWarpDirection}");
                        hasFailedCatastrophically = true;
                        return false;
                    }
                }
                else
                {
                    Logger.Log.WriteError("Bot has no move target");
                    hasFailedCatastrophically = true;
                    return false;
                }
            }
            return true;
        }

        public bool InitializeForMap()
        {
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            Vector2i botLocation = BotUtils.GetSelfLocation();

            Logger.Log.Write($"Reinitializing command for new map (bot: {botLocation}, warpTile: {intraMapWarpTile}, gettingMap: {client.modGlobals.GettingMap}, or-expr: {((intraMapPath.Count == 0) || (currentMapID != bot.Map))}");
            // we're either just starting or have just loaded into a new map
            // so we have to find a path from this map to the warp point of the next map
            nextMapID = interMapPath.Dequeue();
            Logger.Log.Write($"Saw bot move to new map {bot.Map} from {currentMapID}. Now determining how to reach next map {nextMapID}...");
            currentMapID = bot.Map;
            if (BotUtils.GetAllWarpTilesToMap(nextMapID, out warpTilesToNextMap, out intraMapWarpDirection) == false)
            {
                Logger.Log.WriteError($"Could not find any warp tiles leading to map {nextMapID} from {currentMapID}.");
                return false;
            }
            // check if we're already at a valid warp tile or if we're right next to it (accepting diagonals hence the 1.5f distance comparison)
            isAtWarpTile = false;
            foreach (Vector2i warpTile in warpTilesToNextMap)
            {
                if ((warpTile == botLocation) || (botLocation.DistanceTo(warpTile) <= 1.5f))
                {
                    Logger.Log.Write($"Bot is already at valid warp tile {warpTile}, setting that as move target and skipping pathfinding.");
                    isAtWarpTile = true;
                    isMovingByPath = false;
                    intraMapWarpTile = warpTile;
                    break;
                }
            }
            // if we're NOT at or next to any warp tile, then find the shortest path to any of them
            isMovingByPath = false;
            if (isAtWarpTile == false)
            {
                intraMapPath.Clear();
                if (BotUtils.FindShortestPathToAny(warpTilesToNextMap, out intraMapPath, out intraMapWarpTile, true) == false)
                {
                    Logger.Log.WriteError($"Could not find a path through map '{bot.Map}' to reach map '{nextMapID}'. Cannot continue.");
                    return false;
                }
                isMovingByPath = true;
                Logger.Log.Write($"Found shortest path to reach map {nextMapID} (length: {intraMapPath.Count}, lastTile: {intraMapPath.DefaultIfEmpty(new Vector2i(-1, -1)).LastOrDefault()})");
            }
            Logger.Log.Write($"Done initializing for map {bot.Map}, target warp tile is at {intraMapWarpTile} in the direction {intraMapWarpDirection}");
            return true;
        }
    }
}
