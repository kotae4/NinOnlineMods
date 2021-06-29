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

        float spellCastTimer = 0f;

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
            else
            {
                // NOTE:
                // doing this here instead of in ctor because another player / another hostile mob might attack us and force change our target which would break the bot logic
                // also, it is necessary to have a target to cast spells
                CheckTargetChanged();

                // check if we can cast any of our hotbar spells
                // NOTE:
                // we can actually skip the hotbar part and just cast directly from the spellbook
                // BUT keeping it this way allows us to kind of prioritize which spells should be cast (by placing those spells on the lower parts of the hotbar)
                if (client.modGlobals.Tick >= spellCastTimer)
                {
                    /*Logger.Log.Write("BotCommand_Attack", "Perform", $"Got permission to cast a spell this tick (target[{targetIndex}]: {target.num}, hp: {target.Vital[(int)client.modEnumerations.Vitals.HP]}) " +
                        $"(npc: {client.modTypes.Npc[target.num].Name.Trim()}, {client.modTypes.Npc[target.num].HP})");
                    */
                    for (int hotbarIndex = 1; hotbarIndex <= 20; hotbarIndex++)
                    {
                        // sType of 2 indicates it's a spell (aka jutsu)
                        if (client.modGlobals.Hotbar[hotbarIndex].sType == 2)
                        {
                            for (int spellIndex = 1; spellIndex <= 40; spellIndex++)
                            {
                                //Logger.Log.Write("BotCommand_Attack", "Perform", $"Saw PlayerSpells[{spellIndex}]={client.modGlobals.PlayerSpells[spellIndex]}, while Hotbar[{hotbarIndex}].Slot={client.modGlobals.Hotbar[hotbarIndex].Slot}");
                                if (client.modGlobals.PlayerSpells[spellIndex] == client.modGlobals.Hotbar[hotbarIndex].Slot)
                                {
                                    //Logger.Log.Write("BotCommand_Attack", "Perform", $"Saw PlayerSpells[{spellIndex}] from Hotbar[{hotbarIndex}], checking if we can cast it");
                                    if (BotUtils.CanCastSpell(spellIndex))
                                    {
                                        BotUtils.CastSpell(spellIndex);
                                        // WARNING:
                                        // i think the server has a bug where it doesn't handle spellcasts properly if they're sent within a certain timeframe
                                        // so we create our own little timer to simulate the natural delay between pressing keys / receiving confirmation from server
                                        spellCastTimer = client.modGlobals.Tick + 200f;
                                        Logger.Log.Write("BotCommand_Attack", "Perform", $"Cast spell {spellIndex}");
                                        // TO-DO:
                                        // revisit this. is it actually necessary or did i just have a bug elsewhere?
                                        // NOTE:
                                        // return immediately otherwise we get in a weird loop where we're constantly trying to cast the same spell. need to let it finish!
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                // NOTE:
                // is it ever possible to do a basic attack immediately (like literally same frame) after casting a spell?
                if (BotUtils.CanAttack())
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
            }
            return true;
        }

        void CheckTargetChanged()
        {
            if (client.modGlobals.myTarget != targetIndex)
            {
                client.modGlobals.myTarget = targetIndex;
                // NOTE:
                // bot only targets NPCs for now (and probably forever)
                client.modGlobals.myTargetType = Constants.TARGET_TYPE_NPC;
                BotUtils.SetTarget(targetIndex, Constants.TARGET_TYPE_NPC);
            }
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
