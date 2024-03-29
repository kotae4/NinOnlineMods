﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NinMods.Logging;
using NinMods.GameTypeWrappers;

namespace NinMods.Bot
{
    // probably have this class act as a collection of relevant bot commands working toward a specific purpose (farming monsters in this case)
    public class FarmBot
    {
        const int EVT_ITEMDROP_PRIORITY = 100;
        const int EVT_AGGROMOB_PRIORITY = 5;
        const int EVT_MAPLOAD_PRIORITY = 0;

        class InjectedEventData
        {
            public EBotEvent eventType;
            public object eventData;
            public InjectedEventData(EBotEvent _event, object _data)
            {
                eventType = _event;
                eventData = _data;
            }
        }

        bool HasFailedCatastrophically = false;

        enum EBotState
        {
            Idle,
            MovingToMap,
            MovingToHotspot,
            AttackingTarget,
            Healing,
            ChargingChakra,
            CollectingItem
        }

        public enum EBotEvent
        {
            MapLoad,
            AggroMob,
            HealthChanged,
            ItemDrop,
            InventoryItemChanged,
            LevelChanged,
            Death
        }

        EBotState currentState = EBotState.Idle;
        IBotCommand currentCommand = null;
        // this is used for events that need to be handled but shouldn't interrupt the current state
        // in other words, the current state is more important than the injected event
        // in cases where the current state is *less* important, then we simply switch states (interrupting the current state) to handle the event right away
        // in those cases, we do that in the InjectEvent() method directly and wouldn't use this Queue<T>
        // edit: borrowing the priorityqueue class from astar implementation, lol :)
        Pathfinding.PriorityQueue<InjectedEventData> injectedEventQueue = new Pathfinding.PriorityQueue<InjectedEventData>();


        client.modTypes.MapNpcRec targetMonster = null;
        int targetMonsterIndex = 0;
        Stack<Vector2i> targetMonsterPath = null;

        public FarmBot()
        {

        }

        void GetTarget()
        {
            Vector2i botLocation = BotUtils.GetSelfLocation();
            // TO-DO:
            // change this to get nearest monster *that can be pathed to*.
            // currently it just fails if the nearest monster can't be pathed to, and will keep trying that same monster over and over.
            if (BotUtils.GetNearestPathableMonster(botLocation, out targetMonster, out targetMonsterIndex, out targetMonsterPath))
            {
                Vector2i monsterLocation = new Vector2i(targetMonster.X, targetMonster.Y);
                double dist = botLocation.DistanceTo(monsterLocation);
                if (targetMonster.num > 0)
                    Logger.Log.Write($"Got nearest monster '{(targetMonster.num > 0 ? client.modTypes.Npc[targetMonster.num].Name.Trim() : "<null>")}[{targetMonsterIndex}]' at {monsterLocation} ({dist} away)");
            }
            else
            {
                Logger.Log.WriteError("Could not get nearest monster");
                targetMonster = null;
                targetMonsterIndex = 0;
            }
        }

        bool MoveToRestorationState_IfNecessary()
        {
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            float healthPercentage = (float)bot.Vital[(int)client.modEnumerations.Vitals.HP] / (float)bot.MaxVital[(int)client.modEnumerations.Vitals.HP];
            float manaPercentage = (float)bot.Vital[(int)client.modEnumerations.Vitals.MP] / (float)bot.MaxVital[(int)client.modEnumerations.Vitals.MP];
            // TO-DO:
            // don't hardcode this
            if ((manaPercentage <= 0.2f) || (bot.Vital[(int)client.modEnumerations.Vitals.MP] < 10))
            {
                currentCommand = new BotCommand_ChargeChakra();
                currentState = EBotState.ChargingChakra;
                return true;
            }
            if (healthPercentage <= 0.35f)
            {
                currentCommand = new BotCommand_Heal();
                currentState = EBotState.Healing;
                return true;
            }
            return false;
        }

        bool MoveToInjectedState_IfNecessary()
        {
            EBotState oldState = currentState;
            if (injectedEventQueue.Count > 0)
            {
                // NOTE:
                // map load events will never be in the queue because they always interrupt the current state
                InjectedEventData injectedEvent = injectedEventQueue.Dequeue();
                switch (injectedEvent.eventType)
                {
                    case EBotEvent.ItemDrop:
                        {
                            MapItemWrapper mapItem = injectedEvent.eventData as MapItemWrapper;
                            currentCommand = new BotCommand_CollectItem(new Vector2i(mapItem.mapItem.X, mapItem.mapItem.Y));
                            currentState = EBotState.CollectingItem;
                            Logger.Log.WritePipe($"Moved to injected state {currentState} from {oldState}");
                            return true;
                        }
                    case EBotEvent.AggroMob:
                        {
                            MapNpcWrapper mapNpc = injectedEvent.eventData as MapNpcWrapper;
                            currentCommand = new BotCommand_Attack(mapNpc.mapNpc, mapNpc.mapNpcIndex, null);
                            currentState = EBotState.AttackingTarget;
                            Logger.Log.WritePipe($"Moved to injected state {currentState} from {oldState}");
                            return true;
                        }
                }
            }
            return false;
        }

        void NextState()
        {
            EBotState oldState = currentState;
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();

            if (MoveToInjectedState_IfNecessary())
                return;

            // currentState is the 'finished' state, so we're determing what to do next
            switch (currentState)
            {
                case EBotState.MovingToMap:
                case EBotState.MovingToHotspot:
                case EBotState.Healing:
                case EBotState.ChargingChakra:
                    {
                        // we may have finished charging chakra but still need to heal before going into combat again, so check that first
                        if (MoveToRestorationState_IfNecessary())
                            break;
                        // we have arrived at the map (and finished loading), so move to the closest target
                        // or we have arrived at a 'hotspot' on the current map, so move to the closest target
                        // or we have finished healing, so move to closest target
                        // or we have finished charging, so move to closest target
                        GetTarget();
                        if (targetMonster != null)
                        {
                            // if we have a target, start attacking it
                            // NOTE:
                            // this will also move to the target and chase it if it tries running
                            currentCommand = new BotCommand_Attack(targetMonster, targetMonsterIndex, targetMonsterPath);
                            currentState = EBotState.AttackingTarget;
                        }
                        break;
                    }
                case EBotState.AttackingTarget:
                case EBotState.Idle:
                case EBotState.CollectingItem:
                    {
                        // we have killed the target, so check if we need to heal / charge chakra, do that if necessary, otherwise move to closest target
                        // or we're in an idle state in which case we should check our health before engaging
                        // or we finished picking up an item (which could happen immediately after attacking - so we want to check our health still)
                        if (MoveToRestorationState_IfNecessary())
                            break;
                        GetTarget();
                        if (targetMonster != null)
                        {
                            // if we have a target, start attacking it
                            // NOTE:
                            // this will also move to the target and chase it if it tries running
                            currentCommand = new BotCommand_Attack(targetMonster, targetMonsterIndex, targetMonsterPath);
                            currentState = EBotState.AttackingTarget;
                        }
                        break;
                    }
            }
            Logger.Log.WritePipe($"Moved to state {currentState} from {oldState}");
        }

        public void InjectEvent(EBotEvent eventType, object eventData)
        {
            // currently, the only event is the item drop event, and the only state that is more important than that is the 'Attacking' state
            // so, if we're currently 'Attacking' then we add this event to the 'injectedEventQueue' and we'll check that when we complete the 'Attacking' state
            // if we're NOT currently 'Attacking' then we can transition directly to the 'CollectingItem' state
            // NOTE:
            // it was at this point that i realized a proper state machine implementation would be invaluable :)
            // more closely tying events, states (and commands - the logic of a state), and the conditionals that glue it together would be great
            if ((currentState == EBotState.MovingToMap) && (eventType != EBotEvent.Death))
            {
                // just ignore items while we're moving to our grind maps.
                Logger.Log.WritePipe($"Ignoring {eventType} event because we are moving to a new map");
                return;
            }
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            switch (eventType)
            {
                case EBotEvent.ItemDrop:
                    {
                        if (currentState == EBotState.AttackingTarget)
                        {
                            Logger.Log.WritePipe($"Enqueuing {eventType} event because we are in an uninterruptible state");
                            injectedEventQueue.Enqueue(new InjectedEventData(eventType, eventData), EVT_ITEMDROP_PRIORITY);
                            return;
                        }
                        else
                        {
                            Logger.Log.WritePipe($"Interrupting current state to handle {eventType} event");
                            MapItemWrapper mapItem = eventData as MapItemWrapper;
                            currentCommand = new BotCommand_CollectItem(new Vector2i(mapItem.mapItem.X, mapItem.mapItem.Y));
                            currentState = EBotState.CollectingItem;
                        }
                        break;
                    }
                case EBotEvent.AggroMob:
                    {
                        // TO-DO:
                        // figure out how best to keep track of how many mobs we're being attacked by and flee when some threshold is reached
                        if (currentState == EBotState.AttackingTarget)
                        {
                            // if we're already attacking a target, then queue this new target for afterwards
                            Logger.Log.WritePipe($"Enqueuing {eventType} event because we are already attacking a mob");
                            injectedEventQueue.Enqueue(new InjectedEventData(eventType, eventData), EVT_AGGROMOB_PRIORITY);
                            return;
                        }
                        else
                        {
                            // if we're not attacking a target and we're not moving maps, then enter combat immediately
                            // TO-DO:
                            // add some logic to enter flee state instead if some criteria is met (low health, too many mobs, maybe even like... mob density being too great, etc)
                            Logger.Log.WritePipe($"Interrupting current state to handle {eventType} event");
                            MapNpcWrapper mapNpc = eventData as MapNpcWrapper;
                            currentCommand = new BotCommand_Attack(mapNpc.mapNpc, mapNpc.mapNpcIndex, null);
                            currentState = EBotState.AttackingTarget;
                        }
                        break;
                    }
                case EBotEvent.MapLoad:
                    {
                        if (bot.Map == (int)eventData)
                        {
                            Logger.Log.Write($"Ignoring injected event {eventType} because we are already at or moving to target map ID {(int)eventData}");
                            return;
                        }
                        Logger.Log.WritePipe($"Interrupting current state to handle {eventType} event");
                        currentCommand = new BotCommand_MoveToMap((int)eventData);
                        currentState = EBotState.MovingToMap;
                        break;
                    }
                case EBotEvent.Death:
                    {
                        Logger.Log.WritePipe($"Interrupting current state to handle {eventType} event");
                        BotUtils.ReleaseSpirit();
                        currentCommand = null;
                        currentState = EBotState.Idle;
                        break;
                    }
            }
        }

        public void Update()
        {
            if (HasFailedCatastrophically)
            {
                Logger.Log.WritePipe("Bot failed catastrophically, cannot do anything.", Logger.ELogType.Error);
                return;
            }

            if (currentCommand != null)
            {
                if (currentCommand.Perform() == false)
                {
                    // if the current command fails for some reason then switch to idle and hope we don't face the same issue repeatedly
                    // this is definitely bad, but i want to get my character leveled as quickly as possible so i can develop the bot further
                    // so i'll risk getting stuck in a nasty loop over not having the chance to recover at all
                    Logger.Log.WritePipe("Command failed to perform, moving to Idle and then finding next state");
                    currentState = EBotState.Idle;
                    currentCommand = null;
                    NextState();
                    return;
                }
                if (currentCommand.IsComplete())
                {
                    currentCommand = null;
                    Logger.Log.WritePipe("Completed command!");
                }
            }

            if (currentCommand == null)
            {
                Logger.Log.Write("No command this tick, moving to next state");
                NextState();
            }
        }
    }
}
