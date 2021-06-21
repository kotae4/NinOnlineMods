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

        // for debugging
        public delegate void dDrawWeather();
        public static bool drawWeatherFirstRun = true;
        public static ManagedHooker.HookEntry drawWeatherHook;

        public delegate void dRenderText(SFML.Graphics.Font font, string text, int x, int y, SFML.Graphics.Color color, bool shadow = false, byte textSize = 13, SFML.Graphics.RenderWindow target = null);
        public static dRenderText oRenderText = null;

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

                client.modGraphics.DrawWeather();
                drawWeatherHook = ManagedHooker.HookMethod<dDrawWeather>(typeof(client.modGraphics), "DrawWeather", hk_modGraphics_DrawWeather, 0);
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
            System.Reflection.MethodInfo methodInfo = typeof(client.modText).GetMethod("RenderText", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(SFML.Graphics.Font), typeof(string), typeof(int), typeof(int), typeof(SFML.Graphics.Color), typeof(bool), typeof(byte), typeof(SFML.Graphics.RenderWindow) }, null);
            if (methodInfo == null)
            {
                Logger.Log.WriteError("NinMods.Main", "Initialize", "Could not get RenderText methodinfo");
                return;
            }
            oRenderText = (dRenderText)methodInfo.CreateDelegate(typeof(dRenderText));


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
            try
            {
                if (NinMods.Main.frmPlayerStats == null)
                {
                    Logger.Log.Write("NinMods.Main", "Initialize", "Initializing player stats form", Logger.ELogType.Info, null, false);
                    NinMods.Main.frmPlayerStats = new PlayerStatsForm();
                    NinMods.Main.frmPlayerStats.Show();
                }

                NinMods.Main.frmPlayerStats.UpdatePlayerStats(client.modTypes.Player[client.modGlobals.MyIndex]);
            }
            catch (Exception ex)
            {
                Logger.Log.WriteException("NinMods.Main", "hk_modGameLogic_GameLoop", ex);
            }
            // call original
            NinMods.Main.gameLoopHook.CallOriginalFunction(typeof(void));
        }

        public static void DrawTileTypeOverlay()
        {
            if (oRenderText == null) return;
            try
            {
                for (int i = client.modGlobals.TileView.Left; i <= client.modGlobals.TileView.Right; i++)
                {
                    for (int j = client.modGlobals.TileView.Top; j <= client.modGlobals.TileView.Bottom; j++)
                    {
                        if (client.modGraphics.InViewPort(i, j))
                        {
                            int x = (int)((double)(i * 32 - 4) + 16.0);
                            int y = (int)((double)(j * 32 - 7) + 16.0);
                            if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_BLOCKED)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "B", x, y, client.modText.Dx8Color(12), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_WARP)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "WP", x, y, client.modText.Dx8Color(15), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_ITEM)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "I", x, y, client.modText.Dx8Color(15), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_NPCAVOID)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "NA", x, y, client.modText.Dx8Color(15), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_CHECKPOINT)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "C", x, y, client.modText.Dx8Color(10), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_RESOURCE)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "Br", x, y, client.modText.Dx8Color(2), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_NPCSPAWN)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "NS", x, y, client.modText.Dx8Color(14), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_SHOP)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "SH", x, y, client.modText.Dx8Color(9), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_HOUSE)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "H", x, y, client.modText.Dx8Color(10), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_HEAL)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "HE", x, y, client.modText.Dx8Color(10), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_TRAP)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "T", x, y, client.modText.Dx8Color(12), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_SLIDE)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "SL", x, y, client.modText.Dx8Color(11), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_SOUND)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "SO", x, y, client.modText.Dx8Color(17), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_PLAYERSPAWN)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "PS", x, y, client.modText.Dx8Color(13), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_WATER)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "W", x, y, client.modText.Dx8Color(3), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_NOJUTSU)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "NJ", x, y, client.modText.Dx8Color(1), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_NOWARP)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "NW", x, y, client.modText.Dx8Color(1), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_FIRE)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "F", x, y, client.modText.Dx8Color(12), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_THROUGH)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "TH", x, y, client.modText.Dx8Color(17), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_NOTRAP)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "NT", x, y, client.modText.Dx8Color(17), shadow: false, 13);
                            }
                            else if (client.modTypes.Map.Tile[i, j].Type == Constants.TILE_TYPE_SIT)
                            {
                                NinMods.Main.oRenderText(client.modText.Font[1], "SI", x, y, client.modText.Dx8Color(17), shadow: false, 13);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.WriteException("NinMods.Main", "DrawTileTypeOverlay", ex);
            }
        }

        public static void hk_modGraphics_DrawWeather()
        {
            if (NinMods.Main.drawWeatherFirstRun == true)
            {
                NinMods.Main.drawWeatherFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modGraphics_DrawWeather", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            DrawTileTypeOverlay();

            NinMods.Main.drawWeatherHook.CallOriginalFunction(typeof(void));
        }

        public static void hk_modInput_HandleKeyPresses(SFML.Window.Keyboard.Key keyAscii)
        {
            if (NinMods.Main.handleKeyPressesFirstRun == true)
            {
                NinMods.Main.handleKeyPressesFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modInput_HandleKeyPresses", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }
            //Logger.Log.Write("NinMods.Main", "hk_modInput_HandleKeyPresses", "Saw key '" + keyAscii.ToString() + "'");
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
                NinMods.Main.frmPlayerStats.Show();
            }
            else if (keyAscii == SFML.Window.Keyboard.Key.F3)
            {
                client.clsBuffer clsBuffer2 = new client.clsBuffer();
                clsBuffer2.WriteLong(20);
                client.modClientTCP.SendData(clsBuffer2.ToArray());
                clsBuffer2 = null;
            }
            else
            {
                //Logger.Log.Write("NinMods.Main", "hk_modInput_HandleKeyPresses", "calling original");
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