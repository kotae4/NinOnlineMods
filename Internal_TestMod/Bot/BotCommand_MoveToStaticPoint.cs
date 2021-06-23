using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    public class BotCommand_MoveToStaticPoint : IBotCommand
    {
        bool hasFailedCatastrophically = false;
        bool hasReachedDestination = false;
        bool waitingOnMovement = false;

        Stack<Vector2i> path = null;

        public BotCommand_MoveToStaticPoint(Vector2i destination)
        {
            path = Pathfinder.GetPathTo(destination.x, destination.y);
        }

        public bool IsComplete()
        {
            return ((hasReachedDestination) && (hasFailedCatastrophically == false));
        }

        public bool Perform()
        {
            if (path == null)
                hasFailedCatastrophically = true;
            if (hasFailedCatastrophically)
                return false;

            if (path.Count == 0)
            {
                hasReachedDestination = true;
                return true;
            }
            if (client.modGlobals.tmr25 < client.modGlobals.Tick)
            {
                Logger.Log.Write("BotCommand_MoveToStaticPoint", "Perform", $"Saw tmr25 tick ({client.modGlobals.tmr25} < {client.modGlobals.Tick})", Logger.ELogType.Info, null, true);
                waitingOnMovement = false;
            }

            if (waitingOnMovement == false)
            {
                Logger.Log.Write("BotCommand_MoveToStaticPoint", "Perform", "Got permission to perform movement this tick");
                Vector2i botLocation = BotUtils.GetSelfLocation();
                Vector2i nextTile = path.Pop();
                Vector2i tileDirection = nextTile - botLocation;
                byte gameDir = 255;
                for (int index = 0; index < Vector2i.directions.Length; index++)
                    if (tileDirection == Vector2i.directions[index])
                        gameDir = (byte)index;

                if (gameDir == 255)
                {
                    Logger.Log.WriteError("BotCommand_MoveToStaticPoint", "Perform", $"Could not get direction out of {tileDirection} (self: {botLocation}; nextTile: {nextTile})");
                    hasFailedCatastrophically = true;
                    waitingOnMovement = true;
                    return false;
                }
                Logger.Log.Write("BotCommand_MoveToStaticPoint", "Perform", $"Moving bot from {botLocation} to {nextTile} in direction {gameDir} (tileDir: {tileDirection})", Logger.ELogType.Info, null, true);
                // perform next movement
                // set state before sending packet
                client.modTypes.Player[client.modGlobals.MyIndex].Dir = gameDir;
                client.modTypes.Player[client.modGlobals.MyIndex].Moving = (byte)(gameDir > 3 ? Constants.MOVING_DIAGONAL : Constants.MOVING_RUNNING);
                client.modTypes.Player[client.modGlobals.MyIndex].Running = true;
                // send state to server
                client.modClientTCP.SendPlayerMove();
                // client-side prediction (notice how the game's code is literally hundreds of lines to accomplish the exact same thing?)
                // the game uses 143 lines to do JUST this.
                client.modTypes.Player[client.modGlobals.MyIndex].xOffset = tileDirection.x * 32f;
                client.modTypes.Player[client.modGlobals.MyIndex].yOffset = tileDirection.y * 32f;
                client.modTypes.Player[client.modGlobals.MyIndex].X = (byte)nextTile.x;
                client.modTypes.Player[client.modGlobals.MyIndex].Y = (byte)nextTile.y;
                Logger.Log.Write("BotCommand_MoveToStaticPoint", "Perform", $"Predicted: ({client.modTypes.Player[client.modGlobals.MyIndex].X}, " +
                    $"{client.modTypes.Player[client.modGlobals.MyIndex].Y}) (offset: " +
                    $"{client.modTypes.Player[client.modGlobals.MyIndex].xOffset}, " +
                    $"{client.modTypes.Player[client.modGlobals.MyIndex].yOffset})");
                waitingOnMovement = true;
            }
            else
            {
                Logger.Log.Write("BotCommand_MoveToStaticPoint", "Perform", $"Skipping frame because tmr25 isn't ready yet ({client.modGlobals.tmr25} > {client.modGlobals.Tick})");
            }

            return true;
        }
    }
}