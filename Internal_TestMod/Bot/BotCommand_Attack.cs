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
        int timeOfLastAttack = 0;

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

            if (CanAttack())
            {
                Logger.Log.Write("BotCommand_Attack", "Perform", $"Got permission to perform attack this tick (target[{targetIndex}]: {target.num}, hp: {target.Vital[(int)client.modEnumerations.Vitals.HP]}) " +
                    $"(npc: {client.modTypes.Npc[target.num].Name.Trim()}, {client.modTypes.Npc[target.num].HP})");

                FaceTargetIfNot();

                client.clsBuffer clsBuffer2 = new client.clsBuffer();
                clsBuffer2.WriteLong(20);
                client.modClientTCP.SendData(clsBuffer2.ToArray());
                timeOfLastAttack = (int)client.modGlobals.Tick;
            }

            return true;
        }

        void FaceTargetIfNot()
        {
            Vector2i botLocation = BotUtils.GetSelfLocation();
            Vector2i targetLocation = new Vector2i(target.X, target.Y);
            Vector2i tileDirection = targetLocation - botLocation;
            if (tileDirection == Vector2i.zero)
            {
                Logger.Log.WriteError("BotCommand_Attack", "FaceTargetIfNot", $"Bot is on top of target, not setting any direction. dir: {tileDirection} (self: {botLocation}; target: {targetLocation})");
                return;
            }
            byte gameDir = 255;
            for (int index = 0; index < Vector2i.directions_Eight.Length; index++)
                if (tileDirection == Vector2i.directions_Eight[index])
                    gameDir = (byte)index;

            if (gameDir == 255)
            {
                Logger.Log.WriteError("BotCommand_Attack", "FaceTargetIfNot", $"Could not get direction out of {tileDirection} (self: {botLocation}; target: {targetLocation})");
                hasFailedCatastrophically = true;
                return;
            }
            if (gameDir == client.modTypes.Player[client.modGlobals.MyIndex].Dir)
            {
                Logger.Log.Write("BotCommand_Attack", "FaceTargetIfNot", "Bot is already facing target, no need to send dir packet");
            }
            else
            {
                Logger.Log.Write("BotCommand_Attack", "FaceTargetIfNot", $"Setting bot to face target (was {client.modTypes.Player[client.modGlobals.MyIndex].Dir} now {gameDir})");
                client.modTypes.Player[client.modGlobals.MyIndex].Dir = gameDir;
                client.clsBuffer clsBuffer2 = new client.clsBuffer();
                clsBuffer2.WriteLong(18);
                clsBuffer2.WriteLong(gameDir);
                client.modClientTCP.SendData(clsBuffer2.ToArray());
            }
        }

        bool CanAttack()
        {
            if (client.modGlobals.tmr25 >= client.modGlobals.Tick)
                return false;

            int playerAttackSpeed = client.modDatabase.GetPlayerAttackSpeed(client.modGlobals.MyIndex);
            int nextAttackTime = timeOfLastAttack + playerAttackSpeed + 30;
            // NOTE:
            // we ignore some things because we can be reasonably sure the bot won't be in that state
            // taken from client.modGameLogic.CheckAttack()
            if ((nextAttackTime > client.modGlobals.Tick) || (client.modGlobals.SpellBuffer > 0) || (client.modGameLogic.CanPlayerInteract() == false))
                return false;
            if (client.modTypes.Player[client.modGlobals.MyIndex].EventTimer > client.modGlobals.Tick)
                return false;

            return true;
        }
    }
}
