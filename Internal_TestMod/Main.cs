using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NinOnline;
using System.Reflection;
using NinMods.Pathfinding;
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

        // for auto-login. written by the injector to a config file which is then read by the bootstrapper and sent as argument to InjectedClass.InjectedEntryPoint(...)
        public static string AutoLogin_Username = "";
        public static string AutoLogin_Password = "";

        public static bool HasInitialized = false;
        public static object _lock = 0;

        public static SquareGrid MapPathfindingGrid;

        // farmbot
        public static bool IsBotEnabled = false;
        //public static Bot.FarmBot farmBot = new Bot.FarmBot();
        public static FarmBotBlocMachine farmBotBloc = new FarmBotBlocMachine();
        // for F3 keybind 'move to cursor' logic
        public static Bot.IBotCommand moveToCursorCmd;

        // for determing if a new item has dropped
        public static client.modTypes.MapItemRec[] lastFrameMapItems = new client.modTypes.MapItemRec[256];

        // debugging / visuals
        public static PlayerStatsForm frmPlayerStats = null;

        // net logging
        public static long NetBytesSent = 0;
        public static long NetBytesReceived = 0;

        // for tile overlays (might also do other stuff with it, though)
        public delegate void dRenderText(SFML.Graphics.Font font, string text, int x, int y, SFML.Graphics.Color color, bool shadow = false, byte textSize = 13, SFML.Graphics.RenderWindow target = null);
        public static dRenderText oRenderText = null;

        public static void Initialize()
        {
            NetTimeStarted = DateTime.Now.Ticks;
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
                Logger.Log.WriteError("Could not get RenderText methodinfo");
                return;
            }
            oRenderText = (dRenderText)methodInfo.CreateDelegate(typeof(dRenderText));

            Logger.Log.Write("Installing hooks...", Logger.ELogType.Info, null, false);
            try
            {
                RegisterEventHandlers();
                Logger.Log.Write("Done installing hooks!", Logger.ELogType.Info, null, true);
            }
            catch (Exception ex)
            {
                Logger.Log.WriteException(ex);
            }
            InterMapPathfinding.IntermapPathfinding.Initialize();
            HasInitialized = true;
        }

        static void RegisterEventHandlers()
        {
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnMenuLoop, EHookExecutionState.Pre, typeof(NinMods.Main), "Main_OnMenuLoop_Pre");
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnGameLoop, EHookExecutionState.Pre, typeof(NinMods.Main), "Main_OnGameLoop_Pre");
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnGameLoop, EHookExecutionState.Post, typeof(NinMods.Main), "Main_OnGameLoop_Post");
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnMapLoaded, EHookExecutionState.Post, typeof(NinMods.Main), "Main_OnMapLoaded_Post");
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnCombatMsg, EHookExecutionState.Post, typeof(NinMods.Main), "Main_OnCombatMsg_Post");
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnDraw_Worldspace, EHookExecutionState.Pre, typeof(NinMods.Main), "Main_OnDraw_Worldspace_Pre");
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnDraw_Screenspace, EHookExecutionState.Pre, typeof(NinMods.Main), "Main_OnDraw_Screenspace_Pre");
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnKeyPress, EHookExecutionState.Pre, typeof(NinMods.Main), "Main_OnKeyPress_Pre");
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnSetPlayerAccess, EHookExecutionState.Post, typeof(NinMods.Main), "Main_OnSetPlayerAccess_Post");
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnNetRecv, EHookExecutionState.Post, typeof(NinMods.Main), "Main_OnNetRecv_Post");
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnNetSend, EHookExecutionState.Post, typeof(NinMods.Main), "Main_OnNetSend_Post");

            GameHooks.SetupManagedHookerHooks();
        }

        static void StartGrindBot()
        {
            if (HasInitialized == false) return;
            //farmBot = new Bot.FarmBot();
            client.modTypes.PlayerRec bot = Bot.BotUtils.GetSelf();
            int targetMapID = -1;
            if (bot.Level <= 5)
            {
                // grind larva on map Larva Road (id: 26)
                targetMapID = 26;
            }
            else if (bot.Level <= 10)
            {
                // grind spiders on map Moist Plains (id: 99)
                targetMapID = 99;
            }
            else if (bot.Level <= 18)
            {
                // grind wolves on map Outskirts Dead End (id: 130)
                targetMapID = 130;
            }
            else if (bot.Level <= 26)
            {
                // grind tigers on map Striped Lake (id: 90)
                targetMapID = 90;
            }
            else
            {
                targetMapID = bot.Map;
            }
            if (bot.Map != targetMapID)
            {
                farmBot.InjectEvent(Bot.FarmBot.EBotEvent.MapLoad, targetMapID);
            }
        }

        public static void BeginAutoLogin()
        {
            if ((string.IsNullOrEmpty(NinMods.Main.AutoLogin_Username)) || (string.IsNullOrEmpty(NinMods.Main.AutoLogin_Password)))
            {
                Logger.Log.WriteError("Username and/or password are empty, cannot auto-login");
                return;
            }
            if (client.modInterface.IsWinEntityVisible("winMsgBox") == true)
            {
                client.modGameLogic.ResetMsgBox();
                Logger.Log.Write("Suppressed message box");
            }
            if (client.modGlobals.IsLoggingIn == false)
            {
                Logger.Log.Write($"Trying to auto-login now with username '{NinMods.Main.AutoLogin_Username}' and password '{NinMods.Main.AutoLogin_Password}'");
                client.modGlobals.LoginUsernameText = NinMods.Main.AutoLogin_Username;
                client.modGlobals.LoginPasswordText = NinMods.Main.AutoLogin_Password;
                client.modGeneral.MenuState(2);
                // NOTE:
                // client sends Auth_Login packet (id: 1)
                // if successful, server sends us Auth_ServerDetails (id: 2) [client also starts sending CCheckPing packets here (id: 51) every 1 second]
                // selecting a server then sends Auth_SelectServer packet (id: 2)
                // if success the server sends us Auth_ServerSelect (id: 3)
                // to which the client sends CLogin packet (id: 9)
                // the server then sends a BUNCH of packets initializing game state (honestly way too much...)
                // one packet the server sends in this flurry is SLoginOk (id: 7)
                // and then SPlayerXYMap and SCheckForMap near the end of the flurry
                // to which the client replies with CNeedMap
                // and the server continues with SInGame and a bunch of player and npc-related data (probably specific to the map)
                // WARNING:
                // if the user enters incorrect login details, nothing is received at all, yet the socket remains connected...
                // confirmed my recv packet hook is working, so it really is the case.
            }
            else
            {
                Logger.Log.Write($"Game is in invalid state for auto-login. (IsLogging: {client.modGlobals.IsLogging}, IsLoggingIn: {client.modGlobals.IsLoggingIn}, msgBoxVisible: {client.modInterface.IsWinEntityVisible("winMsgBox")})");
            }
        }

        public static void FinishAutoLogin()
        {
            // client.modAuth.Auth_HandleServerDetails is the method that handles the Auth_ServerDetails packet
            // it instantiates and fills in the client.modAuth.ServerDetails array
            // the index into this array is eventually passed to client.modAuth.ServerSelect(int serverIndex) which sends the Auth_SelectServer packet and finalizes the login process.
            // the status of the server is checked first. it must not be 0 or 2.
            if ((client.modAuth.ServerDetails != null) && (client.modAuth.ServerDetails[1].Status != 0) && (client.modAuth.ServerDetails[1].Status != 2))
            {
                Logger.Log.Write("Selecting first server to finish login process...");
                client.modAuth.ServerSelect(1);
            }
            else
            {
                Logger.Log.WriteError("Cannot select server because ServerDetails are invalid");
            }
        }

        // for auto-login
        public static void Main_OnMenuLoop_Pre()
        {
            if (client.modGlobals.InGame == false)
            {
                // WARNING:
                // TO-DO:
                // if returning to main menu from in-game (or, presumably, if you're kicked / disconnected from the game) then auto-login will fail on the server select screen
                // i think we need to add a timer and only start the auto-login process after like... 10 seconds or something. the game's logout process messes up game state so much.
                BeginAutoLogin();
            }
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
                    Logger.Log.Write($"Saw new item " +
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
                Logger.Log.Write("NinMods.Main", "CheckNewItemDrops", "Sending new item to bot for handling");
                //farmBot.InjectEvent(Bot.FarmBot.EBotEvent.ItemDrop, (object)newItemLocation);
                farmBotBloc.handleEvent(new ItemDroppedEvent(newItemLocation));
            }
        }

        // the heart of the bot. this runs every tick on the game's thread.
        public static void Main_OnGameLoop_Pre()
        {
            // TO-DO:
            // check exactly when game state becomes valid for us.
            // now that we're injecting at game startup rather than once we're fully loaded in-game, we **have** to make sure the game state is valid before running any of our logic
            if ((MapPathfindingGrid == null) && (client.modTypes.Map != null) && (string.IsNullOrEmpty(client.modTypes.Map.Name) == false))
            {
                MapPathfindingGrid = new SquareGrid(client.modTypes.Map, client.modTypes.Map.Tile);
            }
            try
            {
                lock (_lock)
                {
                    if (NinMods.Main.frmPlayerStats == null)
                    {
                        Logger.Log.Write("Initializing player stats form", Logger.ELogType.Info, null, false);
                        NinMods.Main.frmPlayerStats = new PlayerStatsForm();
                        NinMods.Main.frmPlayerStats.Show();
                    }
                    if (NinMods.Main.frmPlayerStats.Visible)
                    {
                        client.modTypes.PlayerRec bot = Bot.BotUtils.GetSelf();
                        NinMods.Main.frmPlayerStats.UpdatePlayerStats(bot);
                    }
                }
                if (IsBotEnabled)
                {
                    farmBotBloc.Run(new StartBotEvent());
                    //farmBot.Update();
                }
                if ((moveToCursorCmd != null) && (moveToCursorCmd.IsComplete() == false))
                {
                    if (moveToCursorCmd.Perform() == false)
                    {
                        Logger.Log.Write("Catastrophic error occurred performing MoveToCursor command");
                        moveToCursorCmd = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.WriteException(ex);
            }
        }

        public static void Main_OnGameLoop_Post()
        {
            // i think we have to call this after the original function
            // too lazy to double check
            CheckNewItemDrops();
        }

        // for updating our pathfinding grid (and probably some other stuff later)
        // this *should* be the best hook for map transitions. player & game state should be fully loaded w/ new map values by time this is called.
        public static void Main_OnMapLoaded_Post(int Index, byte[] data, int StartAddr, int ExtraVar)
        {
            client.modTypes.PlayerRec bot = Bot.BotUtils.GetSelf();
            Logger.Log.Write($"Loaded new map {bot.Map}", Logger.ELogType.Info, null, true);
            if ((client.modTypes.Map != null) && (string.IsNullOrEmpty(client.modTypes.Map.Name) == false))
            {
                MapPathfindingGrid = new SquareGrid(client.modTypes.Map, client.modTypes.Map.Tile);
            }
            else
            {
                Logger.Log.WriteError($"Not loading map into pathfinding grid because it's invalid (map: {client.modTypes.Map}, name: {(client.modTypes.Map == null ? "<invalid>" : client.modTypes.Map.Name)})");
                return;
            }
            if (IsBotEnabled)
            {
                StartGrindBot();
            }
        }

        // for damage log / spell accuracy
        public static void Main_OnCombatMsg_Post(int Index, byte[] data, int StartAddr, int ExtraVar)
        {
            client.clsBuffer clsBuffer2 = new client.clsBuffer(data);
            string text = clsBuffer2.ReadString();
            byte tColor = clsBuffer2.ReadByte();
            Logger.Log.Write($"Saw combat msg {text} with color {tColor}");
        }

        // for drawing tile overlays
        // NOTE:
        // everything drawn here is in world-space (coords are tile coords)
        public static void Main_OnDraw_Worldspace_Pre()
        {
            Utils.DrawTileTypeOverlay();
        }

        // for drawing on the screen
        // NOTE:
        // everything drawn here is in screen-space
        public static void Main_OnDraw_Screenspace_Pre()
        {
            Utils.DrawNetStats();
        }

        // for our keybinds :)
        public static void Main_OnKeyPress_Pre(SFML.Window.Keyboard.Key keyAscii)
        {
            if (keyAscii == SFML.Window.Keyboard.Key.F1)
            {
                // NOTE:
                // F1 is used for testing specific exploits
                // /heal command: does not work
                // attacks bypassing attack speed: does work! (bypassing animation timers, only a slight increase imo)
                // movement bypassing timers: does work! (particularly, bypassing the xOffset and yOffset timers, a very considerable increase)
                Vector2i cursorTileLocation = Utilities.GameUtils.GetTilePosFromCursor();
                moveToCursorCmd = new Bot.BotCommand_ExploitMovementToStaticPoint(cursorTileLocation);
                IsBotEnabled = false;
                // bypassing jutsu timers / limits: does not work (but, bypassing the animation timers like in attack speed might defer a slight advantage)
                // speeding up server-side health regen: doesn't work :( (tried sending 50 ping packets on tmr100 interval, no change)
            }
            else if (keyAscii == SFML.Window.Keyboard.Key.F2)
            {
                lock (_lock)
                {
                    if ((NinMods.Main.frmPlayerStats != null) && (NinMods.Main.frmPlayerStats.Visible))
                        NinMods.Main.frmPlayerStats.Close();
                    NinMods.Main.frmPlayerStats = null;
                }
            }
            else if (keyAscii == SFML.Window.Keyboard.Key.F3)
            {
                IsBotEnabled = !IsBotEnabled;
                if (IsBotEnabled)
                {
                    // if we're enabled it from a disabled state, then just re-instantiate it (alternative is resetting its state but i'm being lazy right now)
                    StartGrindBot();
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
            else if (keyAscii == SFML.Window.Keyboard.Key.F6)
            {
                client.modTypes.PlayerRec bot = Bot.BotUtils.GetSelf();
                bot.Access = 8;
                client.modGameEditors.MapEditorInit();
            }
        }

        // for detecting when admins / GMs enter the map
        public static void Main_OnSetPlayerAccess_Post(int index, int access)
        {
            client.modTypes.PlayerRec bot = NinMods.Bot.BotUtils.GetSelf();
            bot.Access = 8;
        }

        // for logging packets that we receive from the server (and for notifying the bot of certain ones)
        public static void Main_OnNetRecv_Post(byte[] data)
        {
            NinMods.Main.NetBytesReceived += data.Length;

            NinMods.Main.NetBytesReceived += data.Length;

            client.clsBuffer clsBuffer2 = new client.clsBuffer(data);
            int num = clsBuffer2.ReadLong();
            client.modEnumerations.ServerPackets packetID = (client.modEnumerations.ServerPackets)num;
            Logger.Log.WriteNetLog($"RECV packet {packetID} (ID: {num})", Logger.ELogType.Info, null, true);
            // NOTE:
            // special handling for auto-login process.
            // instead of hooking client.modAuth.Auth_HandleServerDetails we'll just check for that packet here
            // it is important that client.modAuth.Auth_HandleServerDetails is called before we reach this point, though.
            // we ensure that's the case w/ the CallOriginalFunction above.
            if (packetID == client.modEnumerations.ServerPackets.Auth_ServerDetails)
            {
                FinishAutoLogin();
            }
        }

        // for logging packets that we send to the server
        public static void Main_OnNetSend_Post(byte[] data, bool auth)
        {
            NinMods.Main.NetBytesSent += data.Length;

            NinMods.Main.NetBytesSent += data.Length;

            client.clsBuffer clsBuffer2 = new client.clsBuffer(data);
            int num = clsBuffer2.ReadLong();
            client.modEnumerations.ClientPackets packetID = (client.modEnumerations.ClientPackets)num;
            Logger.Log.WriteNetLog($"SENT packet {packetID} (ID: {num})", Logger.ELogType.Info, null, true);
        }
    }
}