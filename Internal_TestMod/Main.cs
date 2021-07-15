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
using NinMods.Application.FarmBotBloc;
using NinMods.Utilities;

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
        public delegate void dOpenHandleKeyPresses(SFML.Window.Keyboard.Key keyAscii);
        public static bool handleKeyPressesFirstRun = true;
        public static ManagedHooker.HookEntry handleKeyPressesHook;

        public delegate void dOpenSetPlayerAccess(int index, int access);
        public static bool setPlayerAccessFirstRun = true;
        public static ManagedHooker.HookEntry setPlayerAcessHook;

        public delegate void dOpenGameLoop();
        public static bool gameLoopFirstRun = true;
        public static ManagedHooker.HookEntry gameLoopHook;

        // for making sure we always have the latest tiledata for pathfinding
        public delegate void dHandleMapData(int index, byte[] data, int startAddr, int extraVar);
        public static bool handleMapDataFirstRun = true;
        public static ManagedHooker.HookEntry handleMapDataHook = null;

        public delegate void dLoadMap(int mapID);
        public static bool loadMapFirstRun = true;
        public static ManagedHooker.HookEntry loadMapHook = null;

        // for logging incoming network messages (this is what receives all packets and then dispatches them to the specific packet handlers)
        public delegate void dHandleData(byte[] data);
        public static bool handleDataFirstRun = true;
        public static ManagedHooker.HookEntry handleDataHook = null;

        // for logging outgoing network messages
        public delegate void dSendData(byte[] data, bool auth);
        public static bool sendDataFirstRun = true;
        public static ManagedHooker.HookEntry sendDataHook = null;

        // just for tile overlays. can remove later, probably.
        public delegate void dDrawWeather();
        public static bool drawWeatherFirstRun = true;
        public static ManagedHooker.HookEntry drawWeatherHook = null;

        // for HUD overlays
        public delegate void dDrawGUI();
        public static bool drawGUIFirstRun = true;
        public static ManagedHooker.HookEntry drawGUIHook = null;

        #endregion

        public static SquareGrid MapPathfindingGrid;

        // farmbot
        public static bool IsBotEnabled = false;
        public static Bot.FarmBot farmBot = new Bot.FarmBot();
        // for F3 keybind 'move to cursor' logic
        public static Bot.IBotCommand moveToCursorCmd;

        // for determing if a new item has dropped
        public static client.modTypes.MapItemRec[] lastFrameMapItems = new client.modTypes.MapItemRec[256];

        // debugging / visuals
        public static PlayerStatsForm frmPlayerStats = null;
        // for tile overlays (might also do other stuff with it, though)
        public delegate void dRenderText(SFML.Graphics.Font font, string text, int x, int y, SFML.Graphics.Color color, bool shadow = false, byte textSize = 13, SFML.Graphics.RenderWindow target = null);
        public static dRenderText oRenderText = null;
        // debugging network statistics (making sure we aren't flooding their servers [or encountering a bug that causes their server to flood us...])
        public static long NetBytesSent = 0;
        public static long NetBytesReceived = 0;
        // note: this is set to DateTime.Now.Ticks at startup
        public static long NetTimeStarted = 0;

        public static void Initialize()
        {
            // initialize map items
            for (int itemIndex = 0; itemIndex <= 255; itemIndex++)
            {
                lastFrameMapItems[itemIndex] = new client.modTypes.MapItemRec();
                lastFrameMapItems[itemIndex].X = 0;
                lastFrameMapItems[itemIndex].Y = 0;
                lastFrameMapItems[itemIndex].num = 0;
                lastFrameMapItems[itemIndex].PlayerName = "";
            }

            System.Reflection.MethodInfo methodInfo = typeof(client.modText).GetMethod("RenderText", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(SFML.Graphics.Font), typeof(string), typeof(int), typeof(int), typeof(SFML.Graphics.Color), typeof(bool), typeof(byte), typeof(SFML.Graphics.RenderWindow) }, null);
            if (methodInfo == null)
            {
                Logger.Log.WriteError("NinMods.Main", "Initialize", "Could not get RenderText methodinfo");
                return;
            }
            oRenderText = (dRenderText)methodInfo.CreateDelegate(typeof(dRenderText));

            MapPathfindingGrid = new SquareGrid(client.modTypes.Map.Tile, client.modTypes.Map.MaxX, client.modTypes.Map.MaxY);

            Logger.Log.Write("NinMods.Main", "Initialize", "Installing hooks...", Logger.ELogType.Info, null, false);
            // NOTE:
            // this hook keeps crashing, so i'm going to try forcing JIT compilation here and maybe that'll fix it?
            // logs show minhook pointing to clr!ThePreStub or something, which makes me think it's not JIT compiled by time minhook gets to it
            // although... that shouldn't crash it on the first instance. only from the second on. and that isn't the case. i don't know.
            //hk_modHandleData_HandleSpawnItem(0, null, 0, 0);
            Utils.SetupManagedHookerHooks();
            Logger.Log.Write("NinMods.Main", "Initialize", "Done installing hooks!", Logger.ELogType.Info, null, true);
        }

        public static void CheckNewItemDrops()
        {
            client.modTypes.PlayerRec bot = NinMods.Bot.BotUtils.GetSelf();
            //Logger.Log.Write("NinMods.Main", "CheckNewItemDrops", $"Checking {client.modGlobals.MapItem_HighIndex} map items", Logger.ELogType.Info, null, true);
            List<Vector2i> newItemLocations = new List<Vector2i>();
            for (int itemIndex = 1; itemIndex <= 255; itemIndex++)
            {
                client.modTypes.MapItemRec mapItem = client.modTypes.MapItem[itemIndex];
                if (mapItem.num <= 0)
                {
                    // clear the entry in lastFrameMapItems since it's invalid
                    lastFrameMapItems[itemIndex].X = 0;
                    lastFrameMapItems[itemIndex].Y = 0;
                    lastFrameMapItems[itemIndex].num = 0;
                    lastFrameMapItems[itemIndex].PlayerName = "";
                    continue;
                }
                client.modTypes.MapItemRec oldItem = lastFrameMapItems[itemIndex];
                bool isNew = (((lastFrameMapItems[itemIndex].X != mapItem.X) || (lastFrameMapItems[itemIndex].Y != mapItem.Y) || (lastFrameMapItems[itemIndex].num != mapItem.num))
                    && (mapItem.PlayerName.Trim() == bot.Name.Trim()));
                if (isNew)
                {
                    newItemLocations.Add(new Vector2i(mapItem.X, mapItem.Y));
                    Logger.Log.Write("NinMods.Main", "CheckNewItemDrops", $"Saw new item " +
                            $"(idx {itemIndex}, itemNum {mapItem.num})" +
                            $"\n\t-itemLoc ({mapItem.X}, {mapItem.Y})" +
                            $"\n\t-itemPlayer {mapItem.PlayerName.Trim()}" +
                            $"\n\t-itemMvalue {mapItem.mvalue}", Logger.ELogType.Info, null, true);
                }
                // now that we're done processing, perform the deep copy into lastFrameMapItems
                // only going to copy the fields we actually use
                lastFrameMapItems[itemIndex].X = mapItem.X;
                lastFrameMapItems[itemIndex].Y = mapItem.Y;
                lastFrameMapItems[itemIndex].num = mapItem.num;
                lastFrameMapItems[itemIndex].PlayerName = mapItem.PlayerName;

            }
            foreach (Vector2i newItemLocation in newItemLocations)
            {
                farmBot.InjectEvent(Bot.FarmBot.EBotEvent.ItemDrop, (object)newItemLocation);
            }
        }

        // the heart of the bot. this runs every tick on the game's thread.
        public static void hk_modGameLogic_GameLoop()
        {
            if (NinMods.Main.gameLoopFirstRun == true)
            {
                NinMods.Main.gameLoopFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modGameLogic_GameLoop", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }
            Utils.AttemptRehooking();
            try
            {
                if (NinMods.Main.frmPlayerStats == null)
                {
                    Logger.Log.Write("NinMods.Main", "hk_modGameLogic_GameLoop", "Initializing player stats form", Logger.ELogType.Info, null, false);
                    NinMods.Main.frmPlayerStats = new PlayerStatsForm();
                    NinMods.Main.frmPlayerStats.Show();
                }
                if (NinMods.Main.frmPlayerStats.Visible)
                    NinMods.Main.frmPlayerStats.UpdateStats(client.modTypes.Player[client.modGlobals.MyIndex]);
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

            // i think we have to call this after the original function
            // too lazy to double check
            CheckNewItemDrops();
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

        // for drawing tile overlays
        // NOTE:
        // everything drawn here is in world-space
        public static void hk_modGraphics_DrawWeather()
        {
            if (NinMods.Main.drawWeatherFirstRun == true)
            {
                NinMods.Main.drawWeatherFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modGraphics_DrawWeather", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            Utils.DrawTileTypeOverlay();

            NinMods.Main.drawWeatherHook.CallOriginalFunction(typeof(void));
        }

        // for drawing on the screen
        // NOTE:
        // everything drawn here is in screen-space
        public static void hk_modGraphics_DrawGUI()
        {
            if (NinMods.Main.drawGUIFirstRun == true)
            {
                NinMods.Main.drawGUIFirstRun = false;
                Logger.Log.Write("NinMods.Main", "hk_modGraphics_DrawGUI", "Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            Utils.DrawNetStats();

            NinMods.Main.drawGUIHook.CallOriginalFunction(typeof(void));
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
                // NOTE:
                // F1 is used for testing specific exploits
                // /heal command: does not work
                // attacks bypassing attack speed: does work! (bypassing animation timers, only a slight increase imo)
                // movement bypassing timers: does work! (particularly, bypassing the xOffset and yOffset timers, a very considerable increase)
                // bypassing jutsu timers / limits: does not work (but, bypassing the animation timers like in attack speed might defer a slight advantage)
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
                    //farmBot = new Bot.FarmBot();
                    farmBotBloc = new FarmBotBlocMachine();
                }
            }
            else if (keyAscii == SFML.Window.Keyboard.Key.F4)
            {
                Vector2i cursorTileLocation = Utilities.GameUtils.GetTilePosFromCursor();
                moveToCursorCmd = new Bot.BotCommand_MoveToStaticPoint(cursorTileLocation);
            }
            else if (keyAscii == SFML.Window.Keyboard.Key.F5)
            {
                Utils.DumpMapData();
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
                Logger.Log.Write("NinMods.Main", "hk_modDatabase_SetPlayerAccess", $"saw player[{index}]'{client.modTypes.Player[index].Name}' set to access level ({(NinMods.Utilities.GameUtils.EPlayerAccessType)access}[{access}])");
                NinMods.Main.setPlayerAcessHook.CallOriginalFunction(typeof(void), index, access);
            }
            else
            {
                NinMods.Main.setPlayerAcessHook.CallOriginalFunction(typeof(void), index, 8);
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