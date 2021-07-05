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

        // TIL you can call static methods in base constructor forwarding :)
        public FarmBotBlocMachine() : base(FarmBotIdleState.Get(), FarmBotIdleState.Get())
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
            // WARNING:
            // casting e to derived event type is still an expensive operation in C# (example: e as AttackingMobEvent)
            // this is why abstracting the data off the state / event classes somehow would be better (and then get rid of the event / state classes altogether)
            if (e.EventType == EBotEvent.KilledMobSuccesfully || e.EventType == EBotEvent.HpRestoredEvent || e.EventType == EBotEvent.MpRestoredEvent || e.EventType == EBotEvent.StartBotEvent)
            {
                // if we:
                // killed our target, restored our vitals, or are just starting, then enter the attacking state
                retState = getAttackState(healthPercentage, manaPercentage, mana);          
            }
            else if (e.EventType == EBotEvent.AttackingMobEvent)
            {
                // if we are attacking, then continue attacking
                // NOTE:
                // instantiating a new instance every frame seems really inefficient? can we change it to only instantiate if we're coming from a different state?
                AttackingMobEvent ev = e as AttackingMobEvent;
                retState = FarmBotAttackingTargetState.ReInitialize(ev.targetMonster, ev.targetMonsterIndex);
            }
            else if (e.EventType == EBotEvent.ItemDroppedEvent)
            {
                ItemDroppedEvent ide = e as ItemDroppedEvent;
                retState = FarmBotCollectingItemState.ReInitialize(ide.newItemPosition);
            }
            else if (e.EventType == EBotEvent.CollectingItemEvent)
            {
                CollectingItemEvent ev = e as CollectingItemEvent;
                retState = FarmBotCollectingItemState.ReInitialize(ev.newItemPosition);
            }
            else if (e.EventType == EBotEvent.CollectedItemEvent)
            {
                retState = getAttackState(healthPercentage, manaPercentage, mana);
            }
            else if (e.EventType == EBotEvent.HpRestoringEvent)
            {
                retState = FarmBotHealingState.Get();
            }
            else if (e.EventType == EBotEvent.MpRestoringEvent)
            {
                MpRestoringEvent mre = e as MpRestoringEvent;
                retState = FarmBotChargingChakraState.ReInitialize(mre.realBotMapID);
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
                    retState = FarmBotAttackingTargetState.ReInitialize(targetMonster, targetMonsterIndex);
                }
                else
                {
                    retState = FarmBotIdleState.Get();
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
                return FarmBotHealingState.Get();
            }
            if (!enoughMana(manaPercentage, mana))
            {
                //currentCommand = new BotCommand_ChargeChakra();
                // NOTE:
                // this should only be called once, at the start of the chakra charging process... right?
                // from then on mapEventToState should execute the MpRestoringEvent logic instead of calling getAttackState, so this shouldn't be executed anymore
                client.modTypes.PlayerRec bot = NinMods.Bot.BotUtils.GetSelf();
                return FarmBotChargingChakraState.ReInitialize(bot.Map);
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
