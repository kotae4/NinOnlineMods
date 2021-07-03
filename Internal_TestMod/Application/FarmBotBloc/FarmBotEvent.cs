using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Application.FarmBotBloc
{
    public abstract class FarmBotEvent { }

    class FarmBotFailureEvent : FarmBotEvent
    {
        // maybe add details and do logging here
    }

    class StartBotEvent : FarmBotEvent { }

    class AttackingMobEvent: FarmBotEvent {
        public client.modTypes.MapNpcRec targetMonster;
        public int targetMonsterIndex;

        public AttackingMobEvent(client.modTypes.MapNpcRec targetMonsterP, int targetMonsterIndexP)
        {
            targetMonster = targetMonsterP;
            targetMonsterIndex = targetMonsterIndexP;
        }
    }
    class KilledMobSuccesfullyEvent : FarmBotEvent { }
    class ItemDroppedEvent : FarmBotEvent {

        public Vector2i newItemPosition;

        public ItemDroppedEvent (Vector2i newItemPositionP)
        {
            newItemPosition = newItemPositionP;
        }
    }

    class HpRestoringEvent : FarmBotEvent { }
    class HpRestoredEvent : FarmBotEvent { }

    class MpRestoringEvent : FarmBotEvent 
    {
        public int realBotMapID = -1;
        public MpRestoringEvent(int _realBotMapID)
        {
            realBotMapID = _realBotMapID;
        }
    }
    class MpRestoredEvent : FarmBotEvent { }

    class CollectingItemEvent : FarmBotEvent {
        public Vector2i newItemPosition;

        public CollectingItemEvent(Vector2i newItemPositionP)
        {
            newItemPosition = newItemPositionP;
        }
    }
    class CollectedItemEvent : FarmBotEvent { }
}
