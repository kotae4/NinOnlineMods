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
            if (string.IsNullOrEmpty(mandatoryArgument) == false)
            {
                if (mandatoryArgument.IndexOf(';') == -1)
                {
                    MessageBox.Show("[NinMods] Could not parse username;password from argument. Cannot continue.");
                    return 0;
                }
                string[] argParts = mandatoryArgument.Split(';');
                if ((argParts.Length < 2) || (argParts[1].Length < 3))
                {
                    MessageBox.Show("[NinMods] Could not parse username;password from argument. Cannot continue.");
                    return 0;
                }

                NinMods.Main.AutoLogin_Username = argParts[0];
                NinMods.Main.AutoLogin_Password = argParts[1];
                MessageBox.Show($"[NinMods] Injected!\nUsername: {NinMods.Main.AutoLogin_Username}\nPassword: {NinMods.Main.AutoLogin_Password}");
            }
            else
            {
                MessageBox.Show("[NinMods] Could not parse username;password from argument. Cannot continue.");
                return 0;
            }
            NinMods.Main.Initialize();
            System.Windows.Forms.MessageBox.Show("[NinMods] Exiting!");
            return 0;
        }
    }
}