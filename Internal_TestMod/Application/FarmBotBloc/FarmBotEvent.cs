using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Application.FarmBotBloc
{
    public enum EBotEvent
    {
        StartBotEvent,
        FarmBotFailureEvent,
        AttackingMobEvent,
        KilledMobSuccesfully,
        HpRestoringEvent,
        HpRestoredEvent,
        MpRestoringEvent,
        MpRestoredEvent,
        ItemDroppedEvent,
        CollectingItemEvent,
        CollectedItemEvent,
        MAX
    }

    public abstract class FarmBotEvent
    {
        public readonly EBotEvent EventType;
        protected FarmBotEvent(EBotEvent type) { EventType = type; }
    }

    class FarmBotFailureEvent : FarmBotEvent
    {
        readonly static FarmBotFailureEvent _instance = new FarmBotFailureEvent();
        private FarmBotFailureEvent() : base(EBotEvent.FarmBotFailureEvent) { }
        public static FarmBotFailureEvent Get()
        {
            return _instance;
        }
        // maybe add details and do logging here
    }

    class StartBotEvent : FarmBotEvent
    {
        readonly static StartBotEvent _instance = new StartBotEvent();
        private StartBotEvent() : base(EBotEvent.StartBotEvent) { }
        public static StartBotEvent Get()
        {
            return _instance;
        }
    }

    class AttackingMobEvent: FarmBotEvent
    {
        // boilerplate
        readonly static AttackingMobEvent _instance = new AttackingMobEvent();
        private AttackingMobEvent() : base(EBotEvent.AttackingMobEvent) { }
        public static AttackingMobEvent Get()
        {
            return _instance;
        }

        // actual instance data & reinitialization logic
        public client.modTypes.MapNpcRec targetMonster;
        public int targetMonsterIndex;

        public static AttackingMobEvent ReInitialize(client.modTypes.MapNpcRec targetMonsterP, int targetMonsterIndexP)
        {
            _instance.targetMonster = targetMonsterP;
            _instance.targetMonsterIndex = targetMonsterIndexP;
            return _instance;
        }
    }
    class KilledMobSuccesfullyEvent : FarmBotEvent
    {
        readonly static KilledMobSuccesfullyEvent _instance = new KilledMobSuccesfullyEvent();
        private KilledMobSuccesfullyEvent() : base(EBotEvent.KilledMobSuccesfully) { }
        public static KilledMobSuccesfullyEvent Get()
        {
            return _instance;
        }
    }

    class HpRestoringEvent : FarmBotEvent
    {
        readonly static HpRestoringEvent _instance = new HpRestoringEvent();
        private HpRestoringEvent() : base(EBotEvent.HpRestoringEvent) { }
        public static HpRestoringEvent Get()
        {
            return _instance;
        }
    }
    class HpRestoredEvent : FarmBotEvent
    {
        readonly static HpRestoredEvent _instance = new HpRestoredEvent();
        private HpRestoredEvent() : base(EBotEvent.HpRestoredEvent) { }
        public static HpRestoredEvent Get()
        {
            return _instance;
        }
    }

    class MpRestoringEvent : FarmBotEvent 
    {
        readonly static MpRestoringEvent _instance = new MpRestoringEvent();
        private MpRestoringEvent() : base(EBotEvent.MpRestoringEvent) { }
        public static MpRestoringEvent Get()
        {
            return _instance;
        }

        // actual instance data & reinitialization logic
        public int realBotMapID = -1;
        public static MpRestoringEvent ReInitialize(int _realBotMapID)
        {
            _instance.realBotMapID = _realBotMapID;
            return _instance;
        }
    }
    class MpRestoredEvent : FarmBotEvent
    {
        readonly static MpRestoredEvent _instance = new MpRestoredEvent();
        private MpRestoredEvent() : base(EBotEvent.MpRestoredEvent) { }
        public static MpRestoredEvent Get()
        {
            return _instance;
        }
    }

    class ItemDroppedEvent : FarmBotEvent
    {
        readonly static ItemDroppedEvent _instance = new ItemDroppedEvent();
        private ItemDroppedEvent() : base(EBotEvent.ItemDroppedEvent) { }
        public static ItemDroppedEvent Get()
        {
            return _instance;
        }

        // actual instance data & reinitialization logic
        public Vector2i newItemPosition;
        public static ItemDroppedEvent ReInitialize(Vector2i newItemPositionP)
        {
            _instance.newItemPosition = newItemPositionP;
            return _instance;
        }
    }
    class CollectingItemEvent : FarmBotEvent
    {
        readonly static CollectingItemEvent _instance = new CollectingItemEvent();
        private CollectingItemEvent() : base(EBotEvent.CollectingItemEvent) { }
        public static CollectingItemEvent Get()
        {
            return _instance;
        }

        // actual instance data & reinitialization logic
        public Vector2i newItemPosition;
        public static CollectingItemEvent ReInitialize(Vector2i newItemPositionP)
        {
            _instance.newItemPosition = newItemPositionP;
            return _instance;
        }
    }
    class CollectedItemEvent : FarmBotEvent
    {
        readonly static CollectedItemEvent _instance = new CollectedItemEvent();
        private CollectedItemEvent() : base(EBotEvent.CollectedItemEvent) { }
        public static CollectedItemEvent Get()
        {
            return _instance;
        }
    }
}
