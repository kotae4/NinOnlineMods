using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    public class BotCommand_Attack : IBotCommand
    {
        bool hasFailedCatastrophically = false;
        bool hasKilledTarget = false;

        client.modTypes.MapNpcRec target = null;
        int targetIndex = 0;
        // for chasing the target if it moves before we engage it (happens occasionally)
        Stack<Vector2i> path = null;
        bool isChasingByPath = false;
        // cache optimization
        Vector2i targetLocation = new Vector2i();

        public BotCommand_Attack(client.modTypes.MapNpcRec target, int targetIndex)
        {
            this.target = target;
            this.targetIndex = targetIndex;
        }

        public bool IsComplete()
        {
            return (hasKilledTarget) && (hasFailedCatastrophically == false);
        }

        public bool Perform()
        {
            if (hasFailedCatastrophically) return false;

            if ((target == null) || (client.modTypes.MapNpc[targetIndex] != target) || (target.Vital[(int)client.modEnumerations.Vitals.HP] <= 0))
            {
                hasKilledTarget = true;
                return true;
            }
            Vector2i botLocation = BotUtils.GetSelfLocation();
            targetLocation.x = target.X;
            targetLocation.y = target.Y;
            double dist = botLocation.DistanceTo(targetLocation);
            // TO-DO:
            // don't hardcode this
            if (dist > 1.6f)
            {
                Logger.Log.Write("BotCommand_Attack", "Perform", $"Target has moved out of range, beginning chase now (self: {botLocation}, target: {targetLocation}, dist: {dist}");
                if (ChaseTarget(botLocation, dist) == false)
                {
                    hasFailedCatastrophically = true;
                    return false;
                }
            }
            else if (BotUtils.CanAttack())
            {
                isChasingByPath = false;
                path = null;
                Logger.Log.Write("BotCommand_Attack", "Perform", $"Got permission to perform attack this tick (target[{targetIndex}]: {target.num}, hp: {target.Vital[(int)client.modEnumerations.Vitals.HP]}) " +
                    $"(npc: {client.modTypes.Npc[target.num].Name.Trim()}, {client.modTypes.Npc[target.num].HP})");

                Vector2i tileDirection = targetLocation - botLocation;
                if (BotUtils.FaceDir(tileDirection) == false)
                {
                    // NOTE:
                    // assumes error state is from inability to parse direction (the function might return false from some other condition in the future)
                    Logger.Log.WriteError("BotCommand_Attack", "Perform", $"Could not get direction out of {tileDirection} (self: {botLocation}; target: {targetLocation})");
                    hasFailedCatastrophically = true;
                    return false;
                }
                BotUtils.BasicAttack();
            }
            return true;
        }

        bool ChaseTarget(Vector2i botLocation, double dist)
        {
            if (BotUtils.CanMove())
            {
                // NOTE:
                // small optimization where we avoid running AStar if the target is only one tile away
                if ((dist < 2.0d) && (isChasingByPath == false))
                {
                    Vector2i tileDirection = targetLocation - botLocation;
                    if (BotUtils.MoveDir(tileDirection) == false)
                    {
                        Logger.Log.WriteError("BotCommand_Attack", "ChaseTarget", $"Could not move bot at {botLocation} in direction {tileDirection}");
                        hasFailedCatastrophically = true;
                        return false;
                    }
                    Logger.Log.Write("BotCommand_Attack", "ChaseTarget", "Moved one tile to attack target");
                }
                else if ((path == null) || (path.Count == 0))
                {
                    path = BotUtils.GetPathToMonster(target, botLocation);
                    if (path == null)
                    {
                        Logger.Log.Write("BotCommand_Attack", "ChaseTarget", "Could not recalculate path to monster.");
                        hasFailedCatastrophically = true;
                        return false;
                    }
                    Logger.Log.Write("BotCommand_Attack", "ChaseTarget", "Recalculated path to monster because monster moved");
                    isChasingByPath = true;
                }
                if ((path != null) && (path.Count > 0) && (isChasingByPath))
                {
                    Vector2i nextTile = path.Pop();
                    Vector2i tileDirection = nextTile - botLocation;

                    if (BotUtils.MoveDir(tileDirection) == false)
                    {
                        Logger.Log.WriteError("BotCommand_Attack", "Perform", $"Could not move bot at {botLocation} in direction {tileDirection}");
                        hasFailedCatastrophically = true;
                        return false;
                    }
                    Logger.Log.Write("BotCommand_Attack", "ChaseTarget", "Moved along path to chase target");
                }
            }
            return true;
        }
    }
}
