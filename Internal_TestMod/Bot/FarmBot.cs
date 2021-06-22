using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    // probably have this class act as a collection of relevant bot commands working toward a specific purpose (farming monsters in this case)
    public class FarmBot
    {
        Queue<IBotCommand> commandQueue = new Queue<IBotCommand>();

        public void Update()
        {

        }
    }
}
