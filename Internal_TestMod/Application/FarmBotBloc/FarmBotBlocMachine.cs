using NinMods.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Application.FarmBotBloc
{
    public class FarmBotBlocMachine : BaseBlocMachine<FarmBotState, FarmBotEvent>
    {
        public static client.modTypes.MapNpcRec targetMonster;
        public static int targetMonsterIndex;
        public static client.modTypes.MapItemRec[] lastFrameMapItems = new client.modTypes.MapItemRec[256];

        public FarmBotBlocMachine() :
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

            FarmBotState retState = null;

            if (e is KilledMobSuccesfullyEvent || e is HpRestoredEvent || e is MpRestoredEvent || e is StartBotEvent)
            {
                // if we:
                // killed our target, restored our vitals, or are just starting, then enter the attacking state
                retState = getAttackState(healthPercentage, manaPercentage, mana);          
            }
            else if (e is AttackingMobEvent)
            {
                // if we are attacking, then continue attacking
                // NOTE:
                // instantiating a new instance every frame seems really inefficient? can we change it to only instantiate if we're coming from a different state?
                AttackingMobEvent ev = e as AttackingMobEvent;
                retState = new FarmBotAttackingTargetState(ev.targetMonster, ev.targetMonsterIndex);
            }
            else if (e is ItemDroppedEvent)
            {
                ItemDroppedEvent ide = e as ItemDroppedEvent;
                retState = new FarmBotCollectingItemState(ide.newItemPosition);
            }
            else if (e is CollectingItemEvent)
            {
                CollectingItemEvent ev = e as CollectingItemEvent;
                retState = new FarmBotCollectingItemState(ev.newItemPosition);
            }
            else if (e is CollectedItemEvent)
            {
                retState = getAttackState(healthPercentage, manaPercentage, mana);
            }
            else if (e is HpRestoringEvent)
            {
                retState = new FarmBotHealingState();
            }
            else if (e is MpRestoringEvent)
            {
                MpRestoringEvent mre = e as MpRestoringEvent;
                retState = new FarmBotChargingChakraState(mre.realBotMapID);
            }
            else
            {
                // if there was a failure, get health and mp and attack again
                Logger.Log.Write("FarmBotBloc", "mapEventToState", $"Saw failure event or unhandled event, defaulting to an attacking state");
                retState = getAttackState(healthPercentage, manaPercentage, mana);
            }
            Logger.Log.Write("FarmBotBloc", "mapEventToState", $"Mapped event '{e}' to state '{retState}'");
            return retState;
        }

        public override IBotBlocCommand<FarmBotEvent> mapStateToCommand(FarmBotState state)
        {
            IBotBlocCommand<FarmBotEvent> retCommand = null;

            if (state is FarmBotHealingState)
            {
                retCommand = new BotCommand_Heal();
            }
            else if (state is FarmBotAttackingTargetState)
            {
                // if we are in a attacking state we have the target, cause that's the condition to get in that state
                FarmBotAttackingTargetState attackingState = state as FarmBotAttackingTargetState;
                retCommand = new BotCommand_Attack(
                    attackingState.targetMonster,
                    attackingState.targetMonsterIndex
                    );
            }
            else if (state is FarmBotChargingChakraState)
            {
                FarmBotChargingChakraState chargeChakraState = state as FarmBotChargingChakraState;
                retCommand = new BotCommand_ChargeChakra(chargeChakraState.realBotMapID);
            }
            else if (state is FarmBotCollectingItemState)
            {
                FarmBotCollectingItemState collectItemState = state as FarmBotCollectingItemState;
                retCommand = new BotCommand_CollectItem(collectItemState.newItemPosition);
            }
            else
            {
                // in case of idle state, which is our fallback, command is null and we will do it again
                retCommand = null;
            }
            Logger.Log.Write("FarmBotBloc", "mapStateToCommand", $"Mapped state '{state}' to command '{retCommand}'");
            return retCommand;
        }

        private FarmBotState getAttackState(float healthPercentage, float manaPercentage, float mana)
        {
            FarmBotState retState = null;
            FarmBotState vitalsCheckState = getStateForHealthAndMana(healthPercentage, manaPercentage, mana);
            if(vitalsCheckState != null)
            {
                retState = vitalsCheckState;
            }
            else
            {
                //  get target and attack!
                GetTarget();
                if (targetMonster != null)
                {
                    retState = new FarmBotAttackingTargetState(targetMonster, targetMonsterIndex);
                }
                else
                {
                    retState = new FarmBotIdleState();
                }
            }
            Logger.Log.Write("FarmBotBloc", "getAttackState", $"Returning state '{retState}' as most viable attacking state (hpPct {healthPercentage}, mpPct {manaPercentage}, mp {mana})");
            return retState;
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
                // NOTE:
                // this should only be called once, at the start of the chakra charging process... right?
                // from then on mapEventToState should execute the MpRestoringEvent logic instead of calling getAttackState, so this shouldn't be executed anymore
                client.modTypes.PlayerRec bot = NinMods.Bot.BotUtils.GetSelf();
                return new FarmBotChargingChakraState(bot.Map);
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
