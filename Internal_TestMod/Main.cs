using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NinOnline;
using System.Reflection;
using NinMods.Hooking;
using NinMods.Pathfinding;

namespace NinMods
{
    // A NOTE ON GAME TIMERS:
    // modGlobals.tmr25 is probably the most important timer (isConnected, input, movement, attacks, charging chakra, spells, and animations are all done on this timer)
    // the conditional is: if (modGlobals.tmr25 < modGlobals.Tick)
    // the value is set like: modGlobals.tmr25 = (int)(modGlobals.Tick + 25);
    // modGlobals.Tick is set each loop to the value of modGeneral.timer.ElapsedMilliseconds.
    // the 'timer' field there is just a System.Diagnostics.Stopwatch (lol)
    // effectively, this means we need to wait 25 milliseconds after performing any game action
    // i think the easiest way to do this is to perform the same check the game does: if (modGlobals.tmr25 < modGlobals.Tick) { DoNextAction(); }
    public class Main
    {
        public const string MAIN_NAME = "NinMods";
        public const string MAIN_CAPTION = "NinMods";

        #region Hook instance data
        delegate void dOpenHandleKeyPresses(SFML.Window.Keyboard.Key keyAscii);
        public static bool handleKeyPressesFirstRun = true;
        public static ManagedHooker.HookEntry handleKeyPressesHook;

        delegate void dOpenSetPlayerAccess(int index, int access);
        public static bool setPlayerAccessFirstRun = true;
        public static ManagedHooker.HookEntry setPlayerAcessHook;

        delegate void dOpenGameLoop();
        public static bool gameLoopFirstRun = true;
        public static ManagedHooker.HookEntry gameLoopHook;

        // for making sure we always have the latest tiledata for pathfinding
        delegate void dHandleMapData(int index, byte[] data, int startAddr, int extraVar);
        public static bool handleMapDataFirstRun = true;
        public static ManagedHooker.HookEntry handleMapDataHook = null;

        delegate void dLoadMap(int mapID);
        public static bool loadMapFirstRun = true;
        public static ManagedHooker.HookEntry loadMapHook = null;

        delegate void dHandleSpawnItem(int Index, byte[] data, int StartAddr, int ExtraVar);
        public static bool handleSpawnItemFirstRun = true;
        public static ManagedHooker.HookEntry handleSpawnItemHook = null;

        // for logging incoming network messages (this is what receives all packets and then dispatches them to the specific packet handlers)
        delegate void dHandleData(byte[] data);
        public static bool handleDataFirstRun = true;
        public static ManagedHooker.HookEntry handleDataHook = null;

        // for logging outgoing network messages
        delegate void dSendData(byte[] data, bool auth);
        public static bool sendDataFirstRun = true;
        public static ManagedHooker.HookEntry sendDataHook = null;

        // just for tile overlays. can remove later, probably.
        public delegate void dDrawWeather();
        public static bool drawWeatherFirstRun = true;
        public static ManagedHooker.HookEntry drawWeatherHook;
        #endregion

        public static SquareGrid MapPathfindingGrid;

        // farmbot
        public static bool IsBotEnabled = false;
        public static Bot.FarmBot farmBot = new Bot.FarmBot();
        // for F3 keybind 'move to cursor' logic
        public static Bot.IBotCommand moveToCursorCmd;


        // debugging / visuals
        public static PlayerStatsForm frmPlayerStats = null;
        // for tile overlays (might also do other stuff with it, though)
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

                // WARNING:
                // no way to force the JIT compilation of these two methods..
                try
                {
                    handleMapDataHook = ManagedHooker.HookMethod<dHandleMapData>(typeof(client.modHandleData), "HandleMapData", hk_modHandleData_HandleMapData, 0);
                    loadMapHook = ManagedHooker.HookMethod<dLoadMap>(typeof(client.modDatabase), "LoadMap", hk_modDatabase_LoadMap, 0);

                    handleSpawnItemHook = ManagedHooker.HookMethod<dHandleSpawnItem>(typeof(client.modHandleData), "HandleSpawnItem", hk_modHandleData_HandleSpawnItem, 0);

                    handleDataHook = ManagedHooker.HookMethod<dHandleData>(typeof(client.modHandleData), "HandleData", hk_modHandleData_HandleData, 0);
                    sendDataHook = ManagedHooker.HookMethod<dSendData>(typeof(client.modClientTCP), "SendData", hk_modClientTCP_SendData, 0);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("NinMods.Main", "SetupManagedHookerHooks", ex);
                }
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

            MapPathfindingGrid = new SquareGrid(client.modTypes.Map.Tile, client.modTypes.Map.MaxX, client.modTypes.Map.MaxY);

            Logger.Log.Write("NinMods.Main", "Initialize", "Installing hooks...", Logger.ELogType.Info, null, false);
            SetupManagedHookerHooks();
            Logger.Log.Write("NinMods.Main", "Initialize", "Done installing hooks!", Logger.ELogType.Info, null, true);
        }

        // for methods that can't be forced to JIT compile
        // we have to continuously check if the method exists, and, if so, finally hook it.
        public static void AttemptRehooking()
        {
            if (loadMapHook == null)
            {
                try
                {
                    loadMapHook = ManagedHooker.HookMethod<dLoadMap>(typeof(client.modDatabase), "LoadMap", hk_modDatabase_LoadMap, 0);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("NinMods.Main", "AttemptRehooking", ex);
                }
            }
            if (handleMapDataHook == null)
            {
                try
                {
                    handleMapDataHook = ManagedHooker.HookMethod<dHandleMapData>(typeof(client.modHandleData), "HandleMapData", hk_modHandleData_HandleMapData, 0);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("NinMods.Main", "AttemptRehooking", ex);
                }
            }
            if (handleSpawnItemHook == null)
            {
                try
                {
                    handleSpawnItemHook = ManagedHooker.HookMethod<dHandleSpawnItem>(typeof(client.modHandleData), "HandleSpawnItem", hk_modHandleData_HandleSpawnItem, 0);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("NinMods.Main", "AttemptRehooking", ex);
                }
            }
            if (handleDataHook == null)
            {
                try
                {
                    handleDataHook = ManagedHooker.HookMethod<dHandleData>(typeof(client.modHandleData), "HandleData", hk_modHandleData_HandleData, 0);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("NinMods.Main", "AttemptRehooking", ex);
                }
            }
            if (sendDataHook == null)
            {
                try
                {
                    sendDataHook = ManagedHooker.HookMethod<dSendData>(typeof(client.modClientTCP), "SendData", hk_modClientTCP_SendData, 0);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("NinMods.Main", "AttemptRehooking", ex);
                }
            }
        }

        // the heart of the bot. this runs every tick on the game's thread.
        public static void hk_modGameLogic_GameLoop()
        {
            if (NinMods.Main.handleKeyPressesFirstRun == true)
            {
                NinMods.Main.handleKeyPressesFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modGameLogic_GameLoop", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }
            AttemptRehooking();
            try
            {
                if (NinMods.Main.frmPlayerStats == null)
                {
                    Logger.Log.Write("NinMods.Main", "hk_modGameLogic_GameLoop", "Initializing player stats form", Logger.ELogType.Info, null, false);
                    NinMods.Main.frmPlayerStats = new PlayerStatsForm();
                    NinMods.Main.frmPlayerStats.Show();
                }
                if (NinMods.Main.frmPlayerStats.Visible)
                    NinMods.Main.frmPlayerStats.UpdatePlayerStats(client.modTypes.Player[client.modGlobals.MyIndex]);
                /*
                Logger.Log.Write("NinMods.Main", "hk_modGameLogic_GameLoop", $"Player pos: " +
                    $"{client.modTypes.Player[client.modGlobals.MyIndex].X}, {client.modTypes.Player[client.modGlobals.MyIndex].Y} " +
                    $"(index: {client.modGlobals.MyIndex})");
                */
                if (IsBotEnabled)
                    farmBot.Update();

                if ((moveToCursorCmd != null) && (moveToCursorCmd.IsComplete() == false))
                {
                    if (moveToCursorCmd.Perform() == false)
                    {
                        Logger.Log.Write("NinMods.Main", "hk_modGameLogic_GameLoop", "Catastrophic error occurred performing MoveToCursor command");
                        moveToCursorCmd = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.WriteException("NinMods.Main", "hk_modGameLogic_GameLoop", ex);
            }
            // call original
            NinMods.Main.gameLoopHook.CallOriginalFunction(typeof(void));
        }

        // for updating our pathfinding grid (and probably some other stuff later)
        // note: i think this is only called as a result of an admin command? not sure
        public static void hk_modHandleData_HandleMapData(int index, byte[] data, int startAddr, int extraVar)
        {
            if (NinMods.Main.handleMapDataFirstRun == true)
            {
                NinMods.Main.handleMapDataFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modHandleData_HandleMapData", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            NinMods.Main.handleMapDataHook.CallOriginalFunction(typeof(void), index, data, startAddr, extraVar);

            MapPathfindingGrid = new SquareGrid(client.modTypes.Map.Tile, client.modTypes.Map.MaxX, client.modTypes.Map.MaxY);
        }

        // for updating our pathfinding grid (and probably some other stuff later)
        public static void hk_modDatabase_LoadMap(int mapID)
        {
            if (NinMods.Main.loadMapFirstRun == true)
            {
                NinMods.Main.loadMapFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modDatabase_LoadMap", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            NinMods.Main.loadMapHook.CallOriginalFunction(typeof(void), mapID);

            MapPathfindingGrid = new SquareGrid(client.modTypes.Map.Tile, client.modTypes.Map.MaxX, client.modTypes.Map.MaxY);
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

        // for drawing tile overlays
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
                // F1 is used for testing specific exploits
                // /heal command: does not work
                // attacks bypassing attack speed: does not work (but might be very slightly faster?)
                // movement bypassing timers: does work! (particularly, bypassing the xOffset and yOffset timers)
                // bypassing jutsu timers / limits: does not work
                client.clsBuffer clsBuffer2 = new client.clsBuffer();
                clsBuffer2.WriteLong(49);
                // hotbar slot
                clsBuffer2.WriteLong(1);
                // target stuff
                clsBuffer2.WriteByte((byte)client.modGlobals.myTargetType);
                clsBuffer2.WriteLong(client.modGlobals.myTarget);
                client.modClientTCP.SendData(clsBuffer2.ToArray());
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
                IsBotEnabled = !IsBotEnabled;
                if (IsBotEnabled)
                {
                    // if we're enabled it from a disabled state, then just re-instantiate it (alternative is resetting its state but i'm being lazy right now)
                    farmBot = new Bot.FarmBot();
                }
            }
            else if (keyAscii == SFML.Window.Keyboard.Key.F4)
            {
                Vector2i cursorTileLocation = Utilities.GameUtils.GetTilePosFromCursor();
                moveToCursorCmd = new Bot.BotCommand_MoveToStaticPoint(cursorTileLocation);
            }
            else
            {
                //Logger.Log.Write("NinMods.Main", "hk_modInput_HandleKeyPresses", "calling original");
                NinMods.Main.handleKeyPressesHook.CallOriginalFunction(typeof(void), keyAscii);
            }
        }

        // for detecting when admins / GMs enter the map
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

        // for telling the bot to pick up an item drop
        public static void hk_modHandleData_HandleSpawnItem(int Index, byte[] data, int StartAddr, int ExtraVar)
        {
            if (NinMods.Main.handleSpawnItemFirstRun == true)
            {
                NinMods.Main.handleSpawnItemFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modHandleData_HandleSpawnItem", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }
            Logger.Log.Write("NinMods.Main", "hk_modHandleData_HandleSpawnItem", "Calling original HandleSpawnItem function", Logger.ELogType.Info, null, true);
            try
            {
                NinMods.Main.handleSpawnItemHook.CallOriginalFunction(typeof(void), Index, data, StartAddr, ExtraVar);
            }
            catch (Exception ex)
            {
                Logger.Log.WriteException("NinMods.Main", "hk_modHandleData_HandleSpawnItem", ex);
            }
            Logger.Log.Write("NinMods.Main", "hk_modHandleData_HandleSpawnItem", "Sending spawned item into farmbot", Logger.ELogType.Info, null, true);
            // NOTE:
            // clsBuffer doesn't modify the original data buffer, so this is perfectly valid even after calling the original game function
            try
            {
                client.clsBuffer clsBuffer2 = new client.clsBuffer(data);
                int num = clsBuffer2.ReadLong();
                if (client.modTypes.MapItem[num].num > 0)
                {
                    try
                    {
                        client.modTypes.ItemRec item = client.modTypes.Item[client.modTypes.MapItem[num].num];
                        Logger.Log.Write("NinMods.Main", "hk_modHandleData_HandleSpawnItem", $"Saw spawned item '{item.Name}' (ID: {num})", Logger.ELogType.Info, null, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.WriteException("NinMods.Main", "hk_modHandleData_HandleSpawnItem", ex);
                    }
                    Vector2i itemLocation = new Vector2i(client.modTypes.MapItem[num].X, client.modTypes.MapItem[num].Y);
                    farmBot.InjectEvent(Bot.FarmBot.EBotEvent.ItemDrop, itemLocation);
                }
                else
                {
                    Logger.Log.Write("NinMods.Main", "hk_modHandleData_HandleSpawnItem", "Saw invalid spawned item?", Logger.ELogType.Info);
                }
            }
            catch (Exception outerEx)
            {
                Logger.Log.WriteException("NinMods.Main", "hk_modHandleData_HandleSpawnItem", outerEx);
            }
        }

        // for logging packets that we receive from the server (and for notifying the bot of certain ones)
        public static void hk_modHandleData_HandleData(byte[] data)
        {
            if (NinMods.Main.handleDataFirstRun == true)
            {
                NinMods.Main.handleDataFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modHandleData_HandleData", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            client.clsBuffer clsBuffer2 = new client.clsBuffer(data);
            int num = clsBuffer2.ReadLong();
            client.modEnumerations.ServerPackets packetID = (client.modEnumerations.ServerPackets)num;
            Logger.Log.WriteNetLog("NinMods.Main", "hk_modHandleData_HandleData", $"RECV packet {packetID} (ID: {num})", Logger.ELogType.Info, null, true);

            NinMods.Main.handleDataHook.CallOriginalFunction(typeof(void), data);
        }

        // for logging packets that we send to the server
        public static void hk_modClientTCP_SendData(byte[] data, bool auth)
        {
            if (NinMods.Main.sendDataFirstRun == true)
            {
                NinMods.Main.sendDataFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modClientTCP_SendData", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            client.clsBuffer clsBuffer2 = new client.clsBuffer(data);
            int num = clsBuffer2.ReadLong();
            client.modEnumerations.ClientPackets packetID = (client.modEnumerations.ClientPackets)num;
            Logger.Log.WriteNetLog("NinMods.Main", "hk_modClientTCP_SendData", $"SENT packet {packetID} (ID: {num})", Logger.ELogType.Info, null, true);

            NinMods.Main.sendDataHook.CallOriginalFunction(typeof(void), data, auth);
        }
    }
}