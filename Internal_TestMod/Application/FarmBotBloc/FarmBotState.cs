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
    class FarmBotAttackingTargetState : FarmBotState { }
    class FarmBotHealingState : FarmBotState { }
    class FarmBotChargingChakraState : FarmBotState { }
    class FarmBotCollectingItemState : FarmBotState { }


}
