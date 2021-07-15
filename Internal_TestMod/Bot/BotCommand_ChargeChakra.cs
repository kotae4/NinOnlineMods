using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    public class BotCommand_ChargeChakra : IBotCommand
    {
        int realBotMap = -1;

        // for pathing to nearest non-water tile
        Stack<Vector2i> path = null;

        public bool IsComplete()
        {
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            if (bot.Vital[(int)client.modEnumerations.Vitals.MP] == bot.MaxVital[(int)client.modEnumerations.Vitals.MP])
            {
                // NOTE:
                // see other note in Perform() below.
                bot.Map = realBotMap;
                return true;
            }
            return false;
        }

        public bool Perform()
        {
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            Vector2i botLocation = new Vector2i(bot.X, bot.Y);
            // NOTE:
            // we set bot.Map here to prevent modGameLogic.CheckCharge() from running and canceling our charge because we aren't holding the key down
            if (realBotMap == -1)
            {
                realBotMap = bot.Map;
            }
            bot.Map = 0;
            if (client.modTypes.Map.Tile[bot.X, bot.Y].Type == Constants.TILE_TYPE_WATER)
            {
                // path to a non-water tile first
                if (PathToClosestNonWaterTile(bot, botLocation) == false)
                {
                    Logger.Log.Write("BotCommand_ChargeChakra", "Perform", "Cannot path to non-water tile. Cannot continue.");
                    return new FarmBotFailureEvent();
                }
            }
            if (BotUtils.CanChargeChakra())
            {
                Logger.Log.Write("BotCommand_ChargeChakra", "Perform", $"Sending ChargeChakra packet (bot.chargeChakra: {bot.ChargeChakra})");
                BotUtils.ChargeChakra();
            }
            return true;
        }

        bool PathToClosestNonWaterTile(client.modTypes.PlayerRec bot, Vector2i botLocation)
        {
            if (BotUtils.CanMove())
            {
                if (path == null)
                {
                    Vector2i closestTile;
                    if (TryGetClosestNonWaterTile(bot, out closestTile) == false)
                    {
                        Logger.Log.WriteError("BotCommand_ChargeChakra", "PathToClosestNonWaterTile", "Could not find any non-water tiles on the map.");
                        return false;
                    }
                    path = Pathfinder.GetPathTo(closestTile.x, closestTile.y);
                }
                if (path != null)
                {
                    Vector2i nextTile = path.Pop();
                    Vector2i tileDirection = nextTile - botLocation;

                    if (BotUtils.MoveDir(tileDirection) == false)
                    {
                        Logger.Log.WriteError("BotCommand_ChargeChakra", "PathToClosestNonWaterTile", $"Could not move bot at {botLocation} in direction {tileDirection}");
                        return false;
                    }
                    Logger.Log.Write("BotCommand_ChargeChakra", "PathToClosestNonWaterTile", "Moved along path to non-water tile");
                }
            }
            return true;
        }

        // TO-DO:
        // move this to BotUtils and make it predicate-based instead of hard-coded to check for non-water tiles.
        bool TryGetClosestNonWaterTile(client.modTypes.PlayerRec bot, out Vector2i closestTile)
        {
            Vector2i botLocation = new Vector2i(bot.X, bot.Y);
            Vector2i tileLocation = new Vector2i(1, 1);
            closestTile = new Vector2i(int.MaxValue, int.MaxValue);
            double closestDistance = double.MaxValue;
            for (int tileX = 1; tileX < client.modTypes.Map.MaxX; tileX++)
            {
                for (int tileY = 1; tileY < client.modTypes.Map.MaxY; tileY++)
                {
                    tileLocation.x = tileX;
                    tileLocation.y = tileY;
                    if (client.modTypes.Map.Tile[tileX, tileY].Type != Constants.TILE_TYPE_WATER)
                    {
                        double distance = tileLocation.DistanceTo_Squared(botLocation);
                        if (distance < closestDistance)
                        {
                            closestTile.x = tileLocation.x;
                            closestTile.y = tileLocation.y;
                            closestDistance = distance;
                        }
                    }
                }
            }
            return !(closestDistance == double.MaxValue);
        }
    }
}
