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

        public static bool MoveDir(Vector2i tileDirection)
        {
            Vector2i botLocation = GetSelfLocation();
            Vector2i nextTile = new Vector2i(botLocation.x + tileDirection.x, botLocation.y + tileDirection.y);
            byte gameDir = 255;
            for (int index = 0; index < Vector2i.directions_Eight.Length; index++)
                if (tileDirection == Vector2i.directions_Eight[index])
                    gameDir = (byte)index;

            if (gameDir == 255)
            {
                Logger.Log.WriteError("BotUtils", "MoveDir", $"Could not get direction out of {tileDirection} (self: {botLocation}; nextTile: {nextTile})");
                return false;
            }
            Logger.Log.Write("BotUtils", "MoveDir", $"Moving bot from {botLocation} to {nextTile} in direction {gameDir} (tileDir: {tileDirection})", Logger.ELogType.Info, null, true);
            // perform next movement
            // set state before sending packet
            client.modTypes.Player[client.modGlobals.MyIndex].Dir = gameDir;
            // NOTE:
            // the game has a value (3) for MOVING_DIAGONAL but doesn't seem to implement it anywhere. in fact, using it will cause movement to break.
            client.modTypes.Player[client.modGlobals.MyIndex].Moving = Constants.MOVING_RUNNING;
            client.modTypes.Player[client.modGlobals.MyIndex].Running = true;
            // send state to server
            client.modClientTCP.SendPlayerMove();
            // client-side prediction (notice how the game's code is literally hundreds of lines to accomplish the exact same thing?)
            // the game uses 143 lines to do JUST this.
            client.modTypes.Player[client.modGlobals.MyIndex].xOffset = System.Math.Abs(tileDirection.x * 32f);
            client.modTypes.Player[client.modGlobals.MyIndex].yOffset = System.Math.Abs(tileDirection.y * 32f);
            client.modTypes.Player[client.modGlobals.MyIndex].X = (byte)(botLocation.x + tileDirection.x);
            client.modTypes.Player[client.modGlobals.MyIndex].Y = (byte)(botLocation.y + tileDirection.y);
            Logger.Log.Write("BotUtils", "MoveDir", $"Predicted: ({client.modTypes.Player[client.modGlobals.MyIndex].X}, " +
                $"{client.modTypes.Player[client.modGlobals.MyIndex].Y}) (offset: " +
                $"{client.modTypes.Player[client.modGlobals.MyIndex].xOffset}, " +
                $"{client.modTypes.Player[client.modGlobals.MyIndex].yOffset})");
            return true;
        }

        public static bool FaceDir(Vector2i tileDirection)
        {
            byte gameDir = 255;
            for (int index = 0; index < Vector2i.directions_Eight.Length; index++)
                if (tileDirection == Vector2i.directions_Eight[index])
                    gameDir = (byte)index;

            if (gameDir == 255)
            {
                // NOTE:
                // TO-DO:
                // logging inconsistency. normally log message would be here, instead it's left to the caller.
                return false;
            }
            if (gameDir == client.modTypes.Player[client.modGlobals.MyIndex].Dir)
            {
                Logger.Log.Write("BotUtils", "FaceDir", "Bot is already facing target, no need to send dir packet");
            }
            else
            {
                Logger.Log.Write("BotUtils", "FaceDir", $"Setting bot to face target (was {client.modTypes.Player[client.modGlobals.MyIndex].Dir} now {gameDir})");
                client.modTypes.Player[client.modGlobals.MyIndex].Dir = gameDir;
                client.clsBuffer clsBuffer2 = new client.clsBuffer();
                clsBuffer2.WriteLong(18);
                clsBuffer2.WriteLong(gameDir);
                client.modClientTCP.SendData(clsBuffer2.ToArray());
            }
            return true;
        }

        public static void BasicAttack()
        {
            client.clsBuffer clsBuffer2 = new client.clsBuffer();
            clsBuffer2.WriteLong(20);
            client.modClientTCP.SendData(clsBuffer2.ToArray());
            client.modGlobals.TimeSinceAttack = (int)client.modGlobals.Tick;
        }

        public static bool CanMove()
        {
            if (client.modGlobals.tmr25 >= client.modGlobals.Tick)
            {
                Logger.Log.Write("BotUtils", "CanMove", $"Skipping frame because tmr25 isn't ready yet ({client.modGlobals.tmr25} > {client.modGlobals.Tick})");
                return false;
            }
            // NOTE:
            // taken from client.modGameLogic.CheckMovement()
            if ((client.modTypes.Player[client.modGlobals.MyIndex].Moving > 0) || (client.modTypes.Player[client.modGlobals.MyIndex].DeathTimer > 0))
            {
                Logger.Log.Write("BotUtils", "CanMove", $"Skipping frame because player is in invalid state ({client.modTypes.Player[client.modGlobals.MyIndex].Moving}, {client.modTypes.Player[client.modGlobals.MyIndex].DeathTimer})");
                return false;
            }
            return true;
        }

        public static bool CanAttack()
        {
            if (client.modGlobals.tmr25 >= client.modGlobals.Tick)
                return false;

            int playerAttackSpeed = client.modDatabase.GetPlayerAttackSpeed(client.modGlobals.MyIndex);
            int nextAttackTime = client.modGlobals.TimeSinceAttack + playerAttackSpeed + 30;
            // NOTE:
            // we ignore some things because we can be reasonably sure the bot won't be in that state
            // taken from client.modGameLogic.CheckAttack()
            if ((nextAttackTime > client.modGlobals.Tick) || (client.modGlobals.SpellBuffer > 0) || (client.modGameLogic.CanPlayerInteract() == false))
                return false;
            if (client.modTypes.Player[client.modGlobals.MyIndex].EventTimer > client.modGlobals.Tick)
                return false;

            return true;
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
