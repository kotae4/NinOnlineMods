﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NinMods.Application.FarmBotBloc;

namespace NinMods.Bot
{
    public class BotCommand_CollectItem : IBotBlocCommand<FarmBotEvent>
    {
        bool hasFailedCatastrophically = false;
        bool hasCollectedItem = false;

        Vector2i targetLocation = new Vector2i();
        Stack<Vector2i> path = null;

        public BotCommand_CollectItem(Vector2i location)
        {
            path = Pathfinder.GetPathTo(location.x, location.y);
            targetLocation = location;
        }

        public FarmBotEvent Perform()
        {
            if (path == null) hasFailedCatastrophically = true;
            if (hasFailedCatastrophically) return new FarmBotFailureEvent();

            if (path.Count == 0)
            {
                // we've arrived at the item, now collect it
                BotUtils.CollectItem();
                // TO-DO:
                // wait until verification from server. maybe keep retrying if necessary (probably not a good idea, though).
                return new CollectedItemEvent();
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
                    return new FarmBotFailureEvent();
                }
            }
            return new CollectingItemEvent(targetLocation);
        }
    }
}
