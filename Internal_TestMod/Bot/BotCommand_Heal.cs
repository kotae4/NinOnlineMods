using NinMods.Application.FarmBotBloc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    public class BotCommand_Heal : IBotBlocCommand<FarmBotEvent>
    {
        /*public bool IsComplete()
        {
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            float healthPercentage = (float)bot.Vital[(int)client.modEnumerations.Vitals.HP] / (float)bot.MaxVital[(int)client.modEnumerations.Vitals.HP];
            // TO-DO:
            // don't hardcode this
            return healthPercentage > 0.9f;
        }*/

        public FarmBotEvent Perform()
        {
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            float healthPercentage = (float)bot.Vital[(int)client.modEnumerations.Vitals.HP] / (float)bot.MaxVital[(int)client.modEnumerations.Vitals.HP];
            // TO-DO:
            // don't hardcode this
            if(healthPercentage > 0.9f)
            {
                return new HpRestoredEvent();
            }
            return new HpRestoringEvent();
        }
    }
}
