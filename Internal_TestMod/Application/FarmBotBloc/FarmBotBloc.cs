using NinMods.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Application.FarmBotBloc
{
    class FarmBotBloc : Bloc<FarmBotState, FarmBotEvent>
    {

        client.modTypes.MapNpcRec targetMonster;
        int targetMonsterIndex;

        public FarmBotBloc() : base(new FarmBotIdleState())
        {
            // Initialize variables in constructor
            targetMonster = null;
            targetMonsterIndex = 0;
        }

        override
        public FarmBotState mapEventToState(FarmBotEvent e)
        {
            // Get Player Infos
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();

            float health = (float)bot.Vital[(int)client.modEnumerations.Vitals.HP];
            float mana = (float) bot.Vital[(int)client.modEnumerations.Vitals.MP];

            float healthPercentage = health / (float)bot.MaxVital[(int)client.modEnumerations.Vitals.HP];
            float manaPercentage = mana / (float)bot.MaxVital[(int)client.modEnumerations.Vitals.MP];


            if (e is KilledMobSuccesfullyEvent || e is HpRestoredEvent || e is MpRestoredEvent)
            {
                return getAttackState(healthPercentage, manaPercentage, mana);          
            }

            return new FarmBotIdleState();
        }


        override
        public void triggerCommandBasedOnCurrentState()
        {
            if (currentState is FarmBotHealingState)
            {
                currentCommand = new BotCommand_Attack(targetMonster, targetMonsterIndex);
            }
        }

            

        private FarmBotState getAttackState(float healthPercentage, float manaPercentage, float mana)
        {
            FarmBotState vitalsCheckState = getStateForHealthAndMana(healthPercentage, manaPercentage, mana);
            if(vitalsCheckState != null)
            {
                return vitalsCheckState;
            } 
            else
            {
                //  get target and attack!
                GetTarget();
                if (targetMonster != null)
                {

                    return new FarmBotAttackingTargetState();
                }
            }

            return new FarmBotIdleState();
        }

        private FarmBotState getStateForHealthAndMana( float healthPercentage, float manaPercentage, float mana)
        {
            
            if (!enoughHealth(healthPercentage))
            {
                //currentCommand = new BotCommand_Heal();
                return new FarmBotHealingState();
            }
            if (!enoughMana(manaPercentage, mana))
            {
                //currentCommand = new BotCommand_ChargeChakra();
                return new FarmBotChargingChakraState();
            }

            return null;
        }

        void GetTarget()
        {
            Vector2i botLocation = BotUtils.GetSelfLocation();
            if (BotUtils.GetNearestMonster(botLocation, out targetMonster, out targetMonsterIndex))
            {
                Vector2i monsterLocation = new Vector2i(targetMonster.X, targetMonster.Y);
                double dist = botLocation.DistanceTo(monsterLocation);
                if (targetMonster.num > 0)
                    Logger.Log.Write("FarmBot", "GetTarget", $"Got nearest monster '{(targetMonster.num > 0 ? client.modTypes.Npc[targetMonster.num].Name.Trim() : "<null>")}[{targetMonsterIndex}]' at {monsterLocation} ({dist} away)");
            }
            else
            {
                Logger.Log.WriteError("FarmBot", "GetTarget", "Could not get nearest monster");
                targetMonster = null;
                targetMonsterIndex = 0;
            }
        }

        private bool enoughHealth(float healthPercentage)
        {
            return healthPercentage > 0.2;
        }


        private bool enoughMana(float manaPercentage, float mana)
        {
            return ((manaPercentage > 0.2) || (mana > 10.0));
        }
    }
}
