using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    public static class BotUtils
    {
        public static ECompassDirection GetCompassDirectionFromTo(Vector2i from, Vector2i to)
        {
            Vector2i dir = to - from;
            float maxDot = float.NegativeInfinity;
            int retVal = (int)ECompassDirection.Center;
            for (int index = 0; index < Vector2i.directions.Length; index++)
            {
                float dot = Vector2i.Dot(dir, Vector2i.directions[index]);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    retVal = index;
                }
            }
            return (ECompassDirection)retVal;
        }

        public static bool GetNearestMonster(Vector2i from, out client.modTypes.MapNpcRec nearestMonster, out ECompassDirection directionOfMonster)
        {
            Vector2i npcLocation = new Vector2i(0, 0);
            double distance = double.MaxValue;
            double closestDistance = double.MaxValue;
            // i hate initializing to 'default'. would probably be better to return the index instead.
            nearestMonster = default;
            directionOfMonster = ECompassDirection.Center;
            // NOTE:
            // the game starts the index at 1 for some reason, and also NPC_HighIndex is literally the highest index rather than the count, hence the '<=' comparison
            for (int npcIndex = 1; npcIndex <= client.modGlobals.NPC_HighIndex; npcIndex++)
            {
                npcLocation.x = client.modTypes.MapNpc[npcIndex].X;
                npcLocation.y = client.modTypes.MapNpc[npcIndex].Y;

                distance = from.DistanceTo_Squared(npcLocation);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestMonster = client.modTypes.MapNpc[npcIndex];
                    directionOfMonster = GetCompassDirectionFromTo(from, npcLocation);
                }
            }
            return distance != double.MaxValue;
        }

        public static client.modTypes.PlayerRec GetSelf()
        {
            // TO-DO:
            // add sanity checks (checking if we're in-game and our player has fully loaded)
            return client.modTypes.Player[client.modGlobals.MyIndex];
        }

        public static Vector2i GetSelfLocation()
        {
            // TO-DO:
            // add sanity checks (checking if we're in-game and our player has fully loaded)
            return new Vector2i(client.modTypes.Player[client.modGlobals.MyIndex].X, client.modTypes.Player[client.modGlobals.MyIndex].Y);
        }
    }
}
