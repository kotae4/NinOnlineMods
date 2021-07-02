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

            if (BotUtils.CanMove())
            {
                //Logger.Log.Write("BotCommand_MoveToStaticPoint", "Perform", "Got permission to perform movement this tick");
                Vector2i botLocation = BotUtils.GetSelfLocation();
                Vector2i nextTile = path.Pop();
                Vector2i tileDirection = nextTile - botLocation;

                if (BotUtils.MoveDir(tileDirection) == false)
                {
                    Logger.Log.WriteError("BotCommand_MoveToStaticPoint", "Perform", $"Could not move bot at {botLocation} in direction {tileDirection}");
                    hasFailedCatastrophically = true;
                    return false;
                }
            }
            return true;
        }
    }
}