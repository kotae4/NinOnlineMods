using NinMods.Application.FarmBotBloc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    // KOTAE architecture, I'd like to remove this after we translate everything to the new architecture
    public interface IBotCommand
    {
        /// <summary>
        /// Performs the command. Intended to be called every tick so the command may perform additional tasks once a certain game state is reached.
        /// </summary>
        /// <returns>false if the command failed in an unrecoverable way (should not be performed again)</returns>
        bool Perform();
        /// <summary>
        /// Checks if the command has completed its work.
        /// Commands will often perform some work and then be required to wait on the game to reach some state before continuing.
        /// This signals that the desired game state has been reached and the command has nothing else to do.
        /// </summary>
        /// <returns>true if the command is totally complete (and so the next command can be performed), false if it still has work to perform.</returns>
        bool IsComplete();
    }
}