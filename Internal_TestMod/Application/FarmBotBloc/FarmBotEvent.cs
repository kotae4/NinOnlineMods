using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Application.FarmBotBloc
{
    abstract class FarmBotEvent 
    {
        public FarmBotEvent()
        {

        }
    }

    class ItemDroppedEvent : FarmBotEvent { }

    class KilledMobSuccesfullyEvent : FarmBotEvent { }

    class HpRestoredEvent : FarmBotEvent { }

    class MpRestoredEvent : FarmBotEvent { }

}
