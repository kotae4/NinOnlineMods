using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    // probably have this class act as a collection of relevant bot commands working toward a specific purpose (farming monsters in this case)
    public class FarmBot
    {
        bool HasFailedCatastrophically = false;

        enum EBotState
        {
            Idle,
            MovingToTarget,
            MovingToMap,
            MovingToHotspot,
            AttackingTarget,
            Healing,
            ChargingChakra
        }

        EBotState currentState = EBotState.Idle;
        IBotCommand currentCommand = null;

        client.modTypes.MapNpcRec targetMonster = null;
        ECompassDirection targetDirection;

        public FarmBot()
        {

        }

        void GetTarget()
        {
            Vector2i botLocation = BotUtils.GetSelfLocation();
            ECompassDirection monsterDirection;
            client.modTypes.MapNpcRec nearestMonster;
            if (BotUtils.GetNearestMonster(botLocation, out nearestMonster, out monsterDirection))
            {
                targetMonster = nearestMonster;
                targetDirection = monsterDirection;
            }
            else
            {
                targetMonster = null;
            }
        }

        void NextState()
        {
            // currentState is the 'finished' state, so we're determing what to do next
            switch (currentState)
            {
                case EBotState.MovingToTarget:
                    {
                        // we have arrived at the target, so start attacking it
                        currentCommand = new BotCommand_Attack();
                        currentState = EBotState.AttackingTarget;
                        break;
                    }
                case EBotState.MovingToMap:
                case EBotState.MovingToHotspot:
                case EBotState.Healing:
                case EBotState.ChargingChakra:
                    {
                        // we have arrived at the map (and finished loading), so move to the closest target
                        // or we have arrived at a 'hotspot' on the current map, so move to the closest target
                        // or we have finished healing, so move to closest target
                        // or we have finished charging, so move to closest target
                        GetTarget();
                        if (targetMonster != null)
                        {
                            currentCommand = new BotCommand_MoveToTarget(targetMonster);
                            currentState = EBotState.MovingToTarget;
                        }
                        break;
                    }
                case EBotState.AttackingTarget:
                    {
                        // we have killed the target, so check if we need to heal / charge chakra, do that if necessary, otherwise move to closest target
                        client.modTypes.PlayerRec bot = BotUtils.GetSelf();
                        float healthPercentage = bot.Vital[(int)client.modEnumerations.Vitals.HP] / bot.MaxVital[(int)client.modEnumerations.Vitals.HP];
                        if (healthPercentage <= 0.35f)
                        {
                            currentCommand = new BotCommand_Heal();
                            currentState = EBotState.Healing;
                        }
                        // TO-DO:
                        // check chakra (if we want to support jutsu's - probably do)
                        else
                        {
                            GetTarget();
                            if (targetMonster != null)
                            {
                                currentCommand = new BotCommand_MoveToTarget(targetMonster);
                                currentState = EBotState.MovingToTarget;
                            }
                        }
                        break;
                    }
            }
        }

        public void Update()
        {
            if (HasFailedCatastrophically)
                return;

            if (currentCommand != null)
            {
                HasFailedCatastrophically = !(currentCommand.Perform());
                if (currentCommand.IsComplete())
                {
                    currentCommand = null;
                }
            }

            if (currentCommand == null)
            {
                NextState();
            }
        }
    }
}
