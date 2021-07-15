using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InjectedLibrary
{
    public class InjectedClass
    {
        static int InjectedEntryPoint(string mandatoryArgument)
        {
            MessageBox.Show("[NinMods] Injected!");
            NinMods.Main.Initialize();
            System.Windows.Forms.MessageBox.Show("[NinMods] Exiting!");
            return 0;
        }
    }
}