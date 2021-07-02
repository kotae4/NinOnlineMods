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

        public static client.modTypes.MapNpcRec targetMonster;
        public static int targetMonsterIndex;
        public static client.modTypes.MapItemRec[] lastFrameMapItems = new client.modTypes.MapItemRec[256];


        public FarmBotBloc() : base(new FarmBotIdleState(),new FarmBotIdleState())
        {
            // Initialize variables in constructor
            targetMonster = null;
            targetMonsterIndex = 0;
            // initialize map items
            for (int itemIndex = 0; itemIndex <= 255; itemIndex++)
            {
                lastFrameMapItems[itemIndex] = new client.modTypes.MapItemRec();
                lastFrameMapItems[itemIndex].X = 0;
                lastFrameMapItems[itemIndex].Y = 0;
                lastFrameMapItems[itemIndex].num = 0;
                lastFrameMapItems[itemIndex].PlayerName = "";
            }
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


            if (e is KilledMobSuccesfullyEvent || e is HpRestoredEvent || e is MpRestoredEvent || e is StartBotEvent)
            {
                return getAttackState(healthPercentage, manaPercentage, mana);          
            } else if (e is AttackingMobEvent)
            {
                AttackingMobEvent ev = e as AttackingMobEvent;
                return new FarmBotAttackingTargetState(ev.targetMonster, ev.targetMonsterIndex);
            } else if (e is CollectingItemEvent)
            {
                CollectingItemEvent ev = e as CollectingItemEvent;
                return new FarmBotCollectingItemState(ev.newItemPosition);
            }
            else if (e is CollectedItemEvent)
            {
                return getAttackState(healthPercentage, manaPercentage, mana);
            }
            else if (e is HpRestoringEvent)
            {
                return new FarmBotHealingState();
            }
            
            else if (e is MpRestoringEvent)
            {
                return new FarmBotChargingChakraState();
            }
            else
            {
                // if there was a failure, get health and mp and attack again
                return getAttackState(healthPercentage, manaPercentage, mana);

            }
        }


        override
        public void changeCurrentCommandBasedOnCurrentState()
        {
            if (currentState is FarmBotHealingState)
            {
                currentCommand = new BotCommand_Heal();
            } 
            else if (currentState is FarmBotAttackingTargetState)
            {
                // if we are in a attacking state we have the target, cause that's the condition to get in that state
                FarmBotAttackingTargetState state = currentState as FarmBotAttackingTargetState;
                currentCommand = new BotCommand_Attack(
                    state.targetMonster,
                    state.targetMonsterIndex
                    );
            }
            else if (currentState is FarmBotChargingChakraState)
            {
                currentCommand = new BotCommand_ChargeChakra();
            } 
            else if (currentState is FarmBotCollectingItemState)
            {
                FarmBotCollectingItemState state = currentState as FarmBotCollectingItemState;
                currentCommand = new BotCommand_CollectItem(state.newItemPosition);
            }
            else
            {
                // in case of idle state, which is our fallback, command is null and we will do it again
                currentCommand = null;
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

                    return new FarmBotAttackingTargetState(targetMonster, targetMonsterIndex);
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
