using NinMods.Application.FarmBotBloc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    public interface IBotBlocCommand<TBlocEventType>
    {
        /// <summary>
        /// Performs the command. Intended to be called every tick so the command may perform additional tasks once a certain game state is reached.
        /// </summary>
        /// <returns>false if the command failed in an unrecoverable way (should not be performed again)</returns>
        TBlocEventType Perform();
    }
}
