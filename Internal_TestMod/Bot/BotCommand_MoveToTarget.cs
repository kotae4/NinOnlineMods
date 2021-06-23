using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    public class BotCommand_MoveToTarget : IBotCommand
    {
        bool hasFailedCatastrophically = false;
        bool hasReachedDestination = false;
        bool waitingOnMovement = false;

        Stack<Vector2i> path = null;

        public BotCommand_MoveToTarget(client.modTypes.MapNpcRec target)
        {
            path = Pathfinder.GetPathTo(target.X, target.Y);
        }

        public bool IsComplete()
        {
            return ((hasReachedDestination) && (hasFailedCatastrophically == false));
        }

        public bool Perform()
        {
            if (hasFailedCatastrophically)
                return false;

            if ((waitingOnMovement == false) && (client.modGlobals.tmr25 < client.modGlobals.Tick))
            {
                // perform next movement
                // set state before sending packet
                client.modTypes.Player[client.modGlobals.MyIndex].Dir = 1;
                client.modTypes.Player[client.modGlobals.MyIndex].Moving = 1;
                client.modTypes.Player[client.modGlobals.MyIndex].Running = false;
                // send state to server
                client.modClientTCP.SendPlayerMove();
                // client-side prediction
                client.modTypes.Player[client.modGlobals.MyIndex].yOffset = 32f;
                client.modDatabase.SetPlayerY(client.modGlobals.MyIndex, client.modDatabase.GetPlayerY(client.modGlobals.MyIndex) + 1);
            }
            return true;
        }
    }
}