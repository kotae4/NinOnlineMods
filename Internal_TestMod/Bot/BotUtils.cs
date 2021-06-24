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
            for (int index = 0; index < Vector2i.directions_Eight.Length; index++)
            {
                float dot = Vector2i.Dot(dir, Vector2i.directions_Eight[index]);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    retVal = index;
                }
            }
            return (ECompassDirection)retVal;
        }

        public static bool GetNearestMonster(Vector2i from, out client.modTypes.MapNpcRec nearestMonster, out int nearestMonsterIndex)
        {
            Vector2i npcLocation = new Vector2i(0, 0);
            double distance = double.MaxValue;
            double closestDistance = double.MaxValue;
            // would probably be better to return the index instead.
            nearestMonster = null;
            nearestMonsterIndex = 0;
            // NOTE:
            // the game starts the index at 1 for some reason, and also NPC_HighIndex is literally the highest index rather than the count, hence the '<=' comparison
            for (int npcIndex = 1; npcIndex <= client.modGlobals.NPC_HighIndex; npcIndex++)
            {
                npcLocation.x = client.modTypes.MapNpc[npcIndex].X;
                npcLocation.y = client.modTypes.MapNpc[npcIndex].Y;
                if ((npcLocation.x < 0) || (npcLocation.x > client.modTypes.Map.MaxX) ||
                    (npcLocation.y < 0) || (npcLocation.y > client.modTypes.Map.MaxY))
                    continue;

                distance = from.DistanceTo_Squared(npcLocation);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestMonster = client.modTypes.MapNpc[npcIndex];
                    nearestMonsterIndex = npcIndex;
                }
            }
            return distance != double.MaxValue;
        }

        public static Stack<Vector2i> GetPathToMonster(client.modTypes.MapNpcRec monster, Vector2i fromPos)
        {
            Stack<Vector2i> path = null;
            ECompassDirection idealDir = GetCompassDirectionFromTo(fromPos, new Vector2i(monster.X, monster.Y));
            Vector2i attackingTile = new Vector2i(monster.X + Vector2i.directions_Eight[(int)idealDir].x, monster.Y + Vector2i.directions_Eight[(int)idealDir].y);
            path = Pathfinder.GetPathTo(attackingTile.x, attackingTile.y);
            if (path != null)
            {
                Logger.Log.Write("BotUtils", "GetPathToMonster", $"Returning early, found ideal attacking tile {attackingTile}");
                return path;
            }

            foreach (Vector2i dir in Vector2i.directions_Four)
            {
                attackingTile = new Vector2i(monster.X + dir.x, monster.Y + dir.y);
                path = Pathfinder.GetPathTo(attackingTile.x, attackingTile.y);
                if (path != null)
                {
                    Logger.Log.Write("BotUtils", "GetPathToMonster", $"Returning from loop, found attacking tile {attackingTile}");
                    return path;
                }
            }
            return path;
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
