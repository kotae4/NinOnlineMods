using NinMods.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Application.FarmBotBloc
{
    public class FarmBotBloc : Bloc<FarmBotState, FarmBotEvent>
    {
        public static client.modTypes.MapNpcRec targetMonster;
        public static int targetMonsterIndex;
        public static client.modTypes.MapItemRec[] lastFrameMapItems = new client.modTypes.MapItemRec[256];

        public FarmBotBloc() :
            // base(TBlocStateType startState, TBlocStateType fallbackState)
            base(new FarmBotIdleState(),new FarmBotIdleState())
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

        public override FarmBotState mapEventToState(FarmBotEvent e)
        {
            // Get Player Infos
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();

            float health = (float)bot.Vital[(int)client.modEnumerations.Vitals.HP];
            float mana = (float)bot.Vital[(int)client.modEnumerations.Vitals.MP];

            float healthPercentage = health / (float)bot.MaxVital[(int)client.modEnumerations.Vitals.HP];
            float manaPercentage = mana / (float)bot.MaxVital[(int)client.modEnumerations.Vitals.MP];


            if (e is KilledMobSuccesfullyEvent || e is HpRestoredEvent || e is MpRestoredEvent || e is StartBotEvent)
            {
                // if we:
                // killed our target, restored our vitals, or are just starting, then enter the attacking state
                return getAttackState(healthPercentage, manaPercentage, mana);          
            }
            else if (e is AttackingMobEvent)
            {
                // if we are attacking, then continue attacking
                // NOTE:
                // instantiating a new instance every frame seems really inefficient? can we change it to only instantiate if we're coming from a different state?
                AttackingMobEvent ev = e as AttackingMobEvent;
                return new FarmBotAttackingTargetState(ev.targetMonster, ev.targetMonsterIndex);
            }
            else if (e is CollectingItemEvent)
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

        public override IBotBlocCommand<FarmBotEvent> mapStateToCommand(FarmBotState state)
        {
            if (state is FarmBotHealingState)
            {
                return new BotCommand_Heal();
            }
            else if (state is FarmBotAttackingTargetState)
            {
                // if we are in a attacking state we have the target, cause that's the condition to get in that state
                FarmBotAttackingTargetState attackingState = state as FarmBotAttackingTargetState;
                return new BotCommand_Attack(
                    attackingState.targetMonster,
                    attackingState.targetMonsterIndex
                    );
            }
            else if (state is FarmBotChargingChakraState)
            {
                return new BotCommand_ChargeChakra();
            }
            else if (state is FarmBotCollectingItemState)
            {
                FarmBotCollectingItemState collectItemState = state as FarmBotCollectingItemState;
                return new BotCommand_CollectItem(collectItemState.newItemPosition);
            }
            else
            {
                // in case of idle state, which is our fallback, command is null and we will do it again
                return null;
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

        private bool enoughHealth(float healthPercentage)
        {
            // TO-DO:
            // don't hardcode this
            return healthPercentage > 0.2f;
        }
        
        private bool enoughMana(float manaPercentage, float mana)
        {
            // TO-DO:
            // don't hardcode this
            return ((manaPercentage > 0.2f) && (mana > 10f));
        }

        void GetTarget()
        {
            Vector2i botLocation = BotUtils.GetSelfLocation();
            if (BotUtils.GetNearestMonster(botLocation, out targetMonster, out targetMonsterIndex))
            {
                Vector2i monsterLocation = new Vector2i(targetMonster.X, targetMonster.Y);
                double dist = botLocation.DistanceTo(monsterLocation);
                if (targetMonster.num > 0)
                    Logger.Log.Write("FarmBotBloc", "GetTarget", $"Got nearest monster '{((client.modTypes.Npc[targetMonster.num] != null && client.modTypes.Npc[targetMonster.num].Name != null) ? client.modTypes.Npc[targetMonster.num].Name.Trim() : "<null>")}[{targetMonsterIndex}]' at {monsterLocation} ({dist} away)");
            }
            else
            {
                Logger.Log.WriteError("FarmBotBloc", "GetTarget", "Could not get nearest monster");
                targetMonster = null;
                targetMonsterIndex = 0;
            }
        }
    }
}
