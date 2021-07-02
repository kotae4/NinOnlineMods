using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Application.FarmBotBloc
{
    public abstract class FarmBotState {
        public FarmBotState()
        {

        }
    }

    class FarmBotIdleState : FarmBotState { }
    class FarmBotMovingToMapState : FarmBotState { }
    class FarmBotMovingToHotspotState : FarmBotState { }
    class FarmBotAttackingTargetState : FarmBotState { 
        public client.modTypes.MapNpcRec targetMonster;
        public int targetMonsterIndex;

        public FarmBotAttackingTargetState(client.modTypes.MapNpcRec targetMonsterP, int targetMonsterIndexP)
        {
            targetMonster = targetMonsterP;
            targetMonsterIndex = targetMonsterIndexP;
        }
    }
    class FarmBotHealingState : FarmBotState { }
    class FarmBotChargingChakraState : FarmBotState { }
    class FarmBotCollectingItemState : FarmBotState {
        public Vector2i newItemPosition;

        public FarmBotCollectingItemState(Vector2i newItemPositionP)
        {
            newItemPosition = newItemPositionP;
        }
    }


}
