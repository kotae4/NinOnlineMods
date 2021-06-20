using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NinOnline;
using System.Reflection;
using NinMods.Hooking;

namespace NinMods
{
    public static class Main
    {
        public const string MAIN_NAME = "NinMods";
        public const string MAIN_CAPTION = "NinMods";

        delegate void dOpenHandleKeyPresses(SFML.Window.Keyboard.Key keyAscii);
        public static bool handleKeyPressesFirstRun = true;
        public static ManagedHooker.HookEntry handleKeyPressesHook;

        delegate void dOpenSetPlayerAccess(int index, int access);
        public static bool setPlayerAccessFirstRun = true;
        public static ManagedHooker.HookEntry setPlayerAcessHook;

        delegate void dOpenGameLoop();
        public static bool gameLoopFirstRun = true;
        public static ManagedHooker.HookEntry gameLoopHook;

        public static PlayerStatsForm frmPlayerStats = null;

        public static void SetupManagedHookerHooks()
        {
            try
            {
                // the methods have to be JIT'd to native code before we can hook them
                // normally, we'd use System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle) to force JIT compilation
                // but for some reason (probably due to their method body encryption) this throws an exception
                // so we just call the method ourselves to force the JIT compilation.
                // we also set some state to ensure the method early-exits. we don't actually want the method to do any work.
                client.modGlobals.InMapEditor = true;
                client.modInput.HandleKeyPresses(SFML.Window.Keyboard.Key.W);
                client.modGlobals.InMapEditor = false;
                handleKeyPressesHook = ManagedHooker.HookMethod<dOpenHandleKeyPresses>(typeof(client.modInput), "HandleKeyPresses", hk_modInput_HandleKeyPresses, 0);

                client.modDatabase.SetPlayerAccess(-1, 1);
                setPlayerAcessHook = ManagedHooker.HookMethod<dOpenSetPlayerAccess>(typeof(client.modDatabase), "SetPlayerAccess", hk_modDatabase_SetPlayerAccess, 0);

                // NOTE:
                // no way to force an early exit here. hopefully doesn't cause a packet to be missed.
                client.modGameLogic.GameLoop();
                gameLoopHook = ManagedHooker.HookMethod<dOpenGameLoop>(typeof(client.modGameLogic), "GameLoop", hk_modGameLogic_GameLoop, 0);
            }
            catch (Exception ex)
            {
                Logger.Log.Alert("NinMods.Main", "SetupManagedHookerHooks", "exception '" + ex.GetType().Name + "' occurred: " + ex.Message + "\n\n" + ex.StackTrace, MAIN_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error, Logger.ELogType.Error, null, true);
                return;
            }
            if ((handleKeyPressesHook == null) || (setPlayerAcessHook == null))
            {
                Logger.Log.Alert("NinMods.Main", "SetupManagedHookerHooks", "Could not install hooks for unknown reason", MAIN_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error, Logger.ELogType.Info, null, true);
            }
            else
            {
                Logger.Log.Write("NinMods.Main", "SetupManagedHookerHooks", "Success!", Logger.ELogType.Info, null, true);
            }
        }

        public static void Initialize()
        {
            Logger.Log.Write("NinMods.Main", "Initialize", "Installing hooks...", Logger.ELogType.Info, null, false);
            SetupManagedHookerHooks();
            Logger.Log.Write("NinMods.Main", "Initialize", "Done installing hooks!", Logger.ELogType.Info, null, true);
        }

        public static void hk_modGameLogic_GameLoop()
        {
            if (NinMods.Main.handleKeyPressesFirstRun == true)
            {
                NinMods.Main.handleKeyPressesFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modGameLogic_GameLoop", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }
            if (NinMods.Main.frmPlayerStats == null)
            {
                Logger.Log.Write("NinMods.Main", "Initialize", "Initializing player stats form", Logger.ELogType.Info, null, false);
                NinMods.Main.frmPlayerStats = new PlayerStatsForm();
                NinMods.Main.frmPlayerStats.Show();
            }
            if (NinMods.Main.frmPlayerStats.Visible == false)
                NinMods.Main.frmPlayerStats.Visible = true;

            NinMods.Main.frmPlayerStats.UpdatePlayerStats(client.modTypes.Player[client.modGlobals.MyIndex]);
            // call original
            NinMods.Main.gameLoopHook.CallOriginalFunction(typeof(void));
        }

        public static void hk_modInput_HandleKeyPresses(SFML.Window.Keyboard.Key keyAscii)
        {
            if (NinMods.Main.handleKeyPressesFirstRun == true)
            {
                NinMods.Main.handleKeyPressesFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modInput_HandleKeyPresses", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }
            Logger.Log.Write("NinMods.Main", "hk_modInput_HandleKeyPresses", "Saw key '" + keyAscii.ToString() + "'");
            /*
            case "/admin":
			case "/acp":
				if (modTypes.Player[modGlobals.MyIndex].Access > 2)
                {
                    frmAdmin.InstancePtr.Hide();
                    frmAdmin.InstancePtr.Show();
                }
            */
            if (keyAscii == SFML.Window.Keyboard.Key.F1)
            {
                // set state before sending packet
                client.modTypes.Player[client.modGlobals.MyIndex].Dir = 1;
                client.modTypes.Player[client.modGlobals.MyIndex].Moving = 1;
                client.modTypes.Player[client.modGlobals.MyIndex].Running = false;
                // send state to server
                client.modClientTCP.SendPlayerMove();
                // client-side prediction
                client.modTypes.Player[client.modGlobals.MyIndex].yOffset = 32f;
                client.modDatabase.SetPlayerY(client.modGlobals.MyIndex, client.modDatabase.GetPlayerY(client.modGlobals.MyIndex) + 1);
            }
            else if (keyAscii == SFML.Window.Keyboard.Key.F2)
            {
                if (NinMods.Main.frmPlayerStats == null)
                {
                    Logger.Log.Write("NinMods.Main", "hk_modInput_HandleKeyPresses", "Initializing player stats form", Logger.ELogType.Info, null, false);
                    NinMods.Main.frmPlayerStats = new PlayerStatsForm();
                }
                client.frmAdmin.InstancePtr.Hide();
                client.frmAdmin.InstancePtr.Show();
                // setting owner just to experiment. not necessary.
                NinMods.Main.frmPlayerStats.Owner = client.frmAdmin.InstancePtr;
                NinMods.Main.frmPlayerStats.Show();
            }
            else
            {
                Logger.Log.Write("NinMods.Main", "hk_modInput_HandleKeyPresses", "calling original");
                NinMods.Main.handleKeyPressesHook.CallOriginalFunction(typeof(void), keyAscii);
            }
        }

        public static void hk_modDatabase_SetPlayerAccess(int index, int access)
        {
            if (NinMods.Main.setPlayerAccessFirstRun == true)
            {
                NinMods.Main.setPlayerAccessFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modDatabase_SetPlayerAccess", "successfully hooked!", Logger.ELogType.Info, null, true);
            }
            if (index != client.modGlobals.MyIndex)
            {
                Logger.Log.Write("NinMods.Main", "hk_modDatabase_SetPlayerAccess", "saw player[" + index.ToString() + "] set to access level (" + access.ToString() + ")");
                NinMods.Main.setPlayerAcessHook.CallOriginalFunction(typeof(void), index, access);
            }
            else
            {
                NinMods.Main.setPlayerAcessHook.CallOriginalFunction(typeof(void), index, 8);
            }
        }
    }
}
