using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NinMods.GameTypeWrappers;
using NinMods.Logging;

namespace NinMods
{
    public static class GameState
    {
        // things to track:
        // * collection of NPCs targeting us
        // * our health
        // * collection of item drops belonging to us
        // * XP (for detecting mob kills... very dirty but should be fine until quest bot or item farming bot)
        // * collection of items in our inventory
        // * our ryo (gold, currency)
        // * collection of all living NPCs
        // * our level

        class Snapshot
        {
            public bool IsFresh;

            public int Health;
            public int Currency;
            public int Experience;
            public int CharacterLevel;
            public int DeathTimer;
            public List<MapNpcWrapper> AggroMobs;
            public List<MapNpcWrapper> AllNPCs;
            public List<MapItemWrapper> ItemDrops;
            public List<InventoryItemWrapper> InventoryItems;

            public Snapshot()
            {
                IsFresh = true;

                AggroMobs = new List<MapNpcWrapper>();
                AllNPCs = new List<MapNpcWrapper>();
                ItemDrops = new List<MapItemWrapper>();
                InventoryItems = new List<InventoryItemWrapper>();
                Health = 0;
                Currency = 0;
                Experience = 0;
                CharacterLevel = 0;
                DeathTimer = 0;
            }

            public bool IsEmpty {
                get {
                    return ((AggroMobs.Count == 0) && (AllNPCs.Count == 0) && (ItemDrops.Count == 0) && (InventoryItems.Count == 0)
                        && (Health == 0) && (Currency == 0) && (Experience == 0) && (CharacterLevel == 0) && (DeathTimer == 0));
                }
            }

            /// <summary>
            /// assumes rhs is the newer and lhs is the older
            /// </summary>
            /// <param name="lhs"></param>
            /// <param name="rhs"></param>
            /// <returns>Delta snapshot (contains the differences between the two)</returns>
            public static Snapshot operator-(Snapshot lhs, Snapshot rhs)
            {
                Snapshot deltaSnapshot = new Snapshot();
                deltaSnapshot.Health = rhs.Health - lhs.Health;
                deltaSnapshot.Currency = rhs.Currency - lhs.Currency;
                deltaSnapshot.Experience = rhs.Experience - lhs.Experience;
                deltaSnapshot.CharacterLevel = rhs.CharacterLevel - lhs.CharacterLevel;
                deltaSnapshot.DeathTimer = rhs.DeathTimer - lhs.DeathTimer;
                // we can't rely on elements remaining in the same position within their collection
                // so, unfortunately, we have to compare each element to each other element
                // the Enumerable<T>.Except(Enumable<T> other) method just helps us turn ~10 lines into 1.
                // The set difference of two sets is defined as the members of the first set that don't appear in the second set.
                deltaSnapshot.AggroMobs = rhs.AggroMobs.Except<MapNpcWrapper>(lhs.AggroMobs).ToList();
                deltaSnapshot.AllNPCs = rhs.AllNPCs.Except<MapNpcWrapper>(lhs.AllNPCs).ToList();
                deltaSnapshot.ItemDrops = rhs.ItemDrops.Except<MapItemWrapper>(lhs.ItemDrops).ToList();
                deltaSnapshot.InventoryItems = rhs.InventoryItems.Except<InventoryItemWrapper>(lhs.InventoryItems).ToList();
                // the naive but still surprisingly performant approach:
                /*
                foreach (MapNpcWrapper aggroNpcLHS in lhs.AggroMobs)
                {
                    bool found = false;
                    // compare each LHS element to each RHS element...
                    foreach (MapNpcWrapper aggroNpcRHS in rhs.AggroMobs)
                    {
                        if (aggroNpcLHS == aggroNpcRHS)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found == false)
                    {
                        deltaSnapshot.AggroMobs.Add(aggroNpcLHS);
                    }
                }
                */
                return deltaSnapshot;
            }
        }

        static Snapshot frameStartSnapshot = new Snapshot();

        public static bool SaveSnapshot()
        {
            //Logger.Log.Write("Taking frameStart snapshot");
            return TakeSnapshot(ref frameStartSnapshot);
        }

        public static bool CheckNewState()
        {
            if (frameStartSnapshot.IsFresh == true)
                return false;

            Snapshot currentSnapshot = new Snapshot();
            if (TakeSnapshot(ref currentSnapshot) == false)
                return false;

            Snapshot deltaSnapshot = frameStartSnapshot - currentSnapshot;
            // if there are no differences, then early exit.
            if (deltaSnapshot.IsEmpty)
                return true;

            // log deltas
            Logger.Log.Write($"Saw {deltaSnapshot.AggroMobs.Count} new mobs aggro'd onto us\n" +
                $"Saw {deltaSnapshot.ItemDrops.Count} new item drops belonging to us\n" +
                $"Saw {deltaSnapshot.InventoryItems.Count} changed inventory items\n" +
                $"Saw our health change by {deltaSnapshot.Health}\n" +
                $"Saw our bot level change by {deltaSnapshot.CharacterLevel}\n" +
                $"Saw our bot's experience change by {deltaSnapshot.Experience}\n" +
                $"Saw bot's death timer change by {deltaSnapshot.DeathTimer}");
            // compare AggroMobs and push event to bot (so it can either queue an Attack command or decide to flee)
            if (deltaSnapshot.AggroMobs.Count > 0)
            {
                foreach (MapNpcWrapper mapNpc in deltaSnapshot.AggroMobs)
                {
                    Main.farmBot.InjectEvent(Bot.FarmBot.EBotEvent.AggroMob, mapNpc);
                }
            }
            // compare ItemDrops and push event to bot (so it can queue CollectItem commands)
            if (deltaSnapshot.ItemDrops.Count > 0)
            {
                foreach (MapItemWrapper mapItem in deltaSnapshot.ItemDrops)
                {
                    Main.farmBot.InjectEvent(Bot.FarmBot.EBotEvent.ItemDrop, mapItem);
                }
            }
            // compare InventoryItems and push event to bot (so it can decide if it wants to sell its inventory or if it needs to go shopping)
            if (deltaSnapshot.InventoryItems.Count > 0)
            {
                foreach (InventoryItemWrapper invItem in deltaSnapshot.InventoryItems)
                {
                    Main.farmBot.InjectEvent(Bot.FarmBot.EBotEvent.InventoryItemChanged, invItem);
                }
            }
            // compare Health and push event to bot (we really need a way of figuring out the damage source before this is useful at all...)
            if (deltaSnapshot.Health != 0)
            {
                Main.farmBot.InjectEvent(Bot.FarmBot.EBotEvent.HealthChanged, deltaSnapshot.Health);
            }
            // compare Level and push event to bot (so it can learn new jutsus or auto-select mastery or move to new grind map, etc)
            if (deltaSnapshot.CharacterLevel != 0)
            {
                Main.farmBot.InjectEvent(Bot.FarmBot.EBotEvent.LevelChanged, deltaSnapshot.CharacterLevel);
            }
            // compare experience and... add to kill count stat tracker
            if (deltaSnapshot.Experience != 0)
            {
                // TO-DO:
                // stat tracker
            }
            // death timer
            if (deltaSnapshot.DeathTimer > 0)
            {
                // no data needed for this event
                Main.farmBot.InjectEvent(Bot.FarmBot.EBotEvent.Death, 0);
            }


            return true;
        }

        private static bool TakeSnapshot(ref Snapshot snapshot)
        {
            if ((client.modGlobals.MyIndex <= 0) || (client.modGlobals.MyIndex > client.modConstants.MAX_PLAYERS))
                return false;

            // iterate map npcs
            snapshot.AllNPCs.Clear();
            snapshot.AggroMobs.Clear();
            for (int mapNpcIndex = 1; mapNpcIndex <= client.modGlobals.NPC_HighIndex; mapNpcIndex++)
            {
                if ((client.modTypes.MapNpc[mapNpcIndex].num > 0) && (client.modTypes.MapNpc[mapNpcIndex].num <= client.modConstants.MAX_NPCS)
                    && (client.modTypes.MapNpc[mapNpcIndex].Vital[(int)client.modEnumerations.Vitals.HP] > 0))
                {
                    // if it's alive and points to a valid NpcRec then add it to the All list
                    snapshot.AllNPCs.Add(new MapNpcWrapper(client.modTypes.MapNpc[mapNpcIndex], mapNpcIndex));

                    if ((client.modTypes.MapNpc[mapNpcIndex].target == client.modGlobals.MyIndex)
                        && (client.modTypes.Npc[client.modTypes.MapNpc[mapNpcIndex].num].Village != client.modTypes.Player[client.modGlobals.MyIndex].Village))
                    {
                        // if it's alive, points to valid NpcRec, is targeting us, and is not from our village then add it to the Aggro list
                        snapshot.AggroMobs.Add(new MapNpcWrapper(client.modTypes.MapNpc[mapNpcIndex], mapNpcIndex));
                    }
                }
            }
            // iterate map items
            snapshot.ItemDrops.Clear();
            for (int mapItemIndex = 1; mapItemIndex <= client.modGlobals.MapItem_HighIndex; mapItemIndex++)
            {
                if ((client.modTypes.MapItem[mapItemIndex].num > 0)
                    && (client.modTypes.MapItem[mapItemIndex].PlayerName.Trim() == client.modTypes.Player[client.modGlobals.MyIndex].Name.Trim()))
                {
                    // if it points to a valid ItemRec and belongs to us then add it to the list
                    snapshot.ItemDrops.Add(new MapItemWrapper(client.modTypes.MapItem[mapItemIndex], mapItemIndex));
                }
            }
            // iterate inventory items
            snapshot.InventoryItems.Clear();
            // NOTE:
            // hardcoded 40 (there's no constant for it)
            int maxSlots = (client.modTypes.Player[client.modGlobals.MyIndex].UnlockInventory == 0 ? 40 : client.modConstants.MAX_INV);
            for (int inventorySlotIndex = 1; inventorySlotIndex <= maxSlots; inventorySlotIndex++)
            {
                if ((client.modGlobals.PlayerInv[inventorySlotIndex].num > 0) && (client.modGlobals.PlayerInv[inventorySlotIndex].num <= client.modConstants.MAX_ITEMS)
                    && (client.modTypes.Item[client.modGlobals.PlayerInv[inventorySlotIndex].num].pic > 0))
                {
                    // if it points to a valid ItemRec and that ItemRec has a valid pic index then add it to the list
                    snapshot.InventoryItems.Add(new InventoryItemWrapper(client.modGlobals.PlayerInv[inventorySlotIndex], inventorySlotIndex));
                }
            }
            // set the non-collection data...
            snapshot.Health = client.modTypes.Player[client.modGlobals.MyIndex].Vital[(int)client.modEnumerations.Vitals.HP];
            snapshot.Experience = client.modTypes.Player[client.modGlobals.MyIndex].Exp;
            snapshot.CharacterLevel = client.modTypes.Player[client.modGlobals.MyIndex].Level;
            // NOTE:
            // i think ryo also exists as a hardcoded item, but the field on the PlayerRec seems accurate too and it's much easier to access
            snapshot.Currency = client.modTypes.Player[client.modGlobals.MyIndex].Ryo;
            snapshot.DeathTimer = client.modTypes.Player[client.modGlobals.MyIndex].DeathTimer;

            snapshot.IsFresh = false;
            return true;
        }
    }
}
