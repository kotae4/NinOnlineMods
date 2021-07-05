using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Application.FarmBotBloc
{
    public enum EBotState
    {
        FarmBotIdleState,
        FarmBotMovingToMapState,
        FarmBotMovingToHotspotState,
        FarmBotAttackingTargetState,
        FarmBotHealingState,
        FarmBotChargingChakraState,
        FarmBotCollectingItemState,
        MAX
    }

    public abstract class FarmBotState
    {
        public readonly EBotState StateType;
        protected FarmBotState(EBotState type) { StateType = type; }
    }

    class FarmBotIdleState : FarmBotState
    {
        readonly static FarmBotIdleState _instance = new FarmBotIdleState();
        private FarmBotIdleState() : base(EBotState.FarmBotIdleState) { }
        public static FarmBotIdleState Get()
        {
            return _instance;
        }
    }
    class FarmBotMovingToMapState : FarmBotState
    {
        readonly static FarmBotMovingToMapState _instance = new FarmBotMovingToMapState();
        private FarmBotMovingToMapState() : base(EBotState.FarmBotMovingToMapState) { }
        public static FarmBotMovingToMapState Get()
        {
            return _instance;
        }
    }
    class FarmBotMovingToHotspotState : FarmBotState
    {
        readonly static FarmBotMovingToHotspotState _instance = new FarmBotMovingToHotspotState();
        private FarmBotMovingToHotspotState() : base(EBotState.FarmBotMovingToHotspotState) { }
        public static FarmBotMovingToHotspotState Get()
        {
            return _instance;
        }
    }

    class FarmBotAttackingTargetState : FarmBotState
    {
        readonly static FarmBotAttackingTargetState _instance = new FarmBotAttackingTargetState();
        private FarmBotAttackingTargetState() : base(EBotState.FarmBotAttackingTargetState) { }
        public static FarmBotAttackingTargetState Get()
        {
            return _instance;
        }

        // actual instance data & reinitialization logic
        public client.modTypes.MapNpcRec targetMonster;
        public int targetMonsterIndex;
        public static FarmBotAttackingTargetState ReInitialize(client.modTypes.MapNpcRec targetMonsterP, int targetMonsterIndexP)
        {
            _instance.targetMonster = targetMonsterP;
            _instance.targetMonsterIndex = targetMonsterIndexP;
            return _instance;
        }
    }

    class FarmBotHealingState : FarmBotState
    {
        readonly static FarmBotHealingState _instance = new FarmBotHealingState();
        private FarmBotHealingState() : base(EBotState.FarmBotHealingState) { }
        public static FarmBotHealingState Get()
        {
            return _instance;
        }
    }

    class FarmBotChargingChakraState : FarmBotState 
    {
        readonly static FarmBotChargingChakraState _instance = new FarmBotChargingChakraState();
        private FarmBotChargingChakraState() : base(EBotState.FarmBotChargingChakraState) { }
        public static FarmBotChargingChakraState Get()
        {
            return _instance;
        }

        // actual instance data & reinitialization logic
        public int realBotMapID = -1;
        public static FarmBotChargingChakraState ReInitialize(int _realBotMapID)
        {
            _instance.realBotMapID = _realBotMapID;
            return _instance;
        }
    }

    class FarmBotCollectingItemState : FarmBotState
    {
        readonly static FarmBotCollectingItemState _instance = new FarmBotCollectingItemState();
        private FarmBotCollectingItemState() : base(EBotState.FarmBotCollectingItemState) { }
        public static FarmBotCollectingItemState Get()
        {
            return _instance;
        }

        // actual instance data & reinitialization logic
        public Vector2i newItemPosition;
        public static FarmBotCollectingItemState ReInitialize(Vector2i newItemPositionP)
        {
            _instance.newItemPosition = newItemPositionP;
            return _instance;
        }
    }
}
