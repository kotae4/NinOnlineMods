using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    public class BotCommand_ChargeChakra : IBotCommand
    {
        int realBotMap = -1;

        public bool IsComplete()
        {
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            if (bot.Vital[(int)client.modEnumerations.Vitals.MP] == bot.MaxVital[(int)client.modEnumerations.Vitals.MP])
            {
                // NOTE:
                // see other note in Perform() below.
                bot.Map = realBotMap;
                return true;
            }
            return false;
        }

        public bool Perform()
        {
            client.modTypes.PlayerRec bot = BotUtils.GetSelf();
            // NOTE:
            // we set bot.Map here to prevent modGameLogic.CheckCharge() from running and canceling our charge because we aren't holding the key down
            if (realBotMap == -1)
            {
                realBotMap = bot.Map;
            }
            bot.Map = 0;
            if (BotUtils.CanChargeChakra())
            {
                Logger.Log.Write("BotCommand_ChargeChakra", "Perform", $"Sending ChargeChakra packet (bot.chargeChakra: {bot.ChargeChakra})");
                BotUtils.ChargeChakra();
            }
            return true;
        }
    }
}
