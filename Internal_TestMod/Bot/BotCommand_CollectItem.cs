using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    public class BotCommand_CollectItem : IBotCommand
    {
        bool hasFailedCatastrophically = false;
        bool hasCollectedItem = false;

        Vector2i targetLocation = new Vector2i();
        Stack<Vector2i> path = null;

        public BotCommand_CollectItem(Vector2i location)
        {
            path = Pathfinder.GetPathTo(location.x, location.y);
        }

        public bool IsComplete()
        {
            return hasCollectedItem;
        }

        public bool Perform()
        {
            if (path == null) hasFailedCatastrophically = true;
            if (hasFailedCatastrophically) return false;

            if (path.Count == 0)
            {
                // we've arrived at the item, now collect it
                BotUtils.CollectItem();
                // TO-DO:
                // wait until verification from server. maybe keep retrying if necessary (probably not a good idea, though).
                hasCollectedItem = true;
                return true;
            }
            else if (BotUtils.CanMove())
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
