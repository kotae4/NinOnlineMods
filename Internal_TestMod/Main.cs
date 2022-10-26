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
using NinMods.Logging;

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

        public static bool HasInitializedHooks = false;
        public static bool HasInitializedGame = false;
        public static object _lock = 0;

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

        // net logging
        public static long NetBytesSent = 0;
        public static long NetBytesReceived = 0;

        // for tile overlays (might also do other stuff with it, though)
        public delegate void dRenderText(SFML.Graphics.Font font, string text, int x, int y, SFML.Graphics.Color color, bool shadow = false, byte textSize = 13, SFML.Graphics.RenderWindow target = null);
        public static dRenderText oRenderText = null;

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
                Logger.Log.WriteError("Could not get RenderText methodinfo");
                return;
            }
            oRenderText = (dRenderText)methodInfo.CreateDelegate(typeof(dRenderText));

            Logger.Log.Write("Installing hooks...");
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
            HasInitializedHooks = true;
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

            // just some wild testing
            // action msg is the in-world text that appears above the enemies head when you deal dmg. probably other stuff too.
            GameHooks.RegisterEventHandler(GameHooks.EEventType.OnActionMessage, EHookExecutionState.Post, typeof(NinMods.Main), "Main_OnActionMsg_Post");

            GameHooks.SetupManagedHookerHooks();
        }

        static void StartGrindBot()
        {
            if (HasInitializedHooks == false) return;
            //farmBot = new Bot.FarmBot();
            client.modTypes.PlayerRec bot = Bot.BotUtils.GetSelf();
            int targetMapID = -1;
            if (bot.Village == 1)
            {
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
            }
            else if (bot.Village == 2)
            {
                if (bot.Level <= 7)
                {
                    // grind larva on map Sand Nesting Ground (id: 85)
                    targetMapID = 85;
                }
                else if (bot.Level <= 12)
                {
                    // grind scorpions on map Desert Palms (id: 48)
                    targetMapID = 48;
                }
                else if (bot.Level <= 16)
                {
                    // grind stingers on map Sand Hive Grounds (id: 86)
                    targetMapID = 86;
                }
                else if (bot.Level <= 22)
                {
                    // grind scarabs on map Desert Cave Entrance (id: 87)
                    targetMapID = 87;
                }
                else
                {
                    targetMapID = bot.Map;
                }
            }
            if (bot.Map != targetMapID)
            {
                farmBot.InjectEvent(Bot.FarmBot.EBotEvent.MapLoad, targetMapID);
            }
        }
        /*
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
                // NOTE:
                // should be warning, change when logging levels are actually implemented
                Logger.Log.WritePipe("Suppressed message box", Logger.ELogType.Error);
            }
            if (client.modGlobals.IsLoggingIn == false)
            {
                Logger.Log.WritePipe($"Trying to auto-login now with username '{NinMods.Main.AutoLogin_Username}' and password '{NinMods.Main.AutoLogin_Password}'");
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
                Logger.Log.WritePipe("Selecting first server to finish login process...");
                client.modAuth.ServerSelect(1);
            }
            else
            {
                Logger.Log.WritePipe("Cannot select server because ServerDetails are invalid", Logger.ELogType.Error);
            }
        }
        */

        // for auto-login
        public static void Main_OnMenuLoop_Pre()
        {
            // NOTE:
            // this *must* be called on game thread, hence this is the earliest we can possibly call it.
            // log messages generated before this point can't be sent through the pipe.
            if (Logger.Log.NeedsInit)
            {
                Logger.Log.InitPipe();
            }
            /*
            if (client.modGlobals.InGame == false)
            {
                // WARNING:
                // TO-DO:
                // if returning to main menu from in-game (or, presumably, if you're kicked / disconnected from the game) then auto-login will fail on the server select screen
                // i think we need to add a timer and only start the auto-login process after like... 10 seconds or something. the game's logout process messes up game state so much.
                BeginAutoLogin();
            }
            */
        }

        public static void CheckForAttackingMobs()
        {
            int yOffset = 0;
            Vector2i npcLocation = Vector2i.zero;
            for (int npcIndex = 1; npcIndex <= client.modGlobals.NPC_HighIndex; npcIndex++)
            {
                client.modTypes.MapNpcRec mapNPC = client.modTypes.MapNpc[npcIndex];
                npcLocation.x = mapNPC.X;
                npcLocation.y = mapNPC.Y;
                if ((npcLocation.x < 0) || (npcLocation.x > client.modTypes.Map.MaxX) ||
                    (npcLocation.y < 0) || (npcLocation.y > client.modTypes.Map.MaxY) ||
                    (mapNPC.num <= 0) || (mapNPC.num > client.modConstants.MAX_NPCS)
                    || (mapNPC.Vital[(int)client.modEnumerations.Vitals.HP] <= 0)
                    || (client.modTypes.Npc[mapNPC.num].Village == client.modTypes.Player[client.modGlobals.MyIndex].Village))
                    continue;
                client.modTypes.NpcRec npcData = client.modTypes.Npc[mapNPC.num];

                if (mapNPC.target == client.modGlobals.MyIndex)
                {
                    int isAttacking_TextWidth = client.modText.TextWidth(ref client.modText.Font[1], $"{npcData.Name.Trim()} is attacking");
                    NinMods.Main.oRenderText(client.modText.Font[1], $"{npcData.Name.Trim()} is attacking", client.modGraphics.ScreenWidth - isAttacking_TextWidth, yOffset, SFML.Graphics.Color.Red, false, 13, client.modGraphics.GameWindowForm.Window);
                    yOffset += 14;
                }
            }
        }

        public static void DrawTargetInfo()
        {
            client.modTypes.PlayerRec bot = Bot.BotUtils.GetSelf();
            Vector2i botPos = Bot.BotUtils.GetSelfLocation();
            int xBase = client.modGraphics.ScreenWidth - 300;
            int yOffset = 10;

            if (client.modGlobals.myTarget > 0)
            {
                client.modTypes.MapNpcRec mapNPC = client.modTypes.MapNpc[client.modGlobals.myTarget];
                Vector2i targetPos = new Vector2i(mapNPC.X, mapNPC.Y);
                if ((mapNPC.X < 0) || (mapNPC.X > client.modTypes.Map.MaxX) ||
                    (mapNPC.Y < 0) || (mapNPC.Y > client.modTypes.Map.MaxY) ||
                    (mapNPC.num <= 0) || (mapNPC.num > 255)
                    || (mapNPC.Vital[(int)client.modEnumerations.Vitals.HP] <= 0))
                    return;
                client.modTypes.NpcRec npcData = client.modTypes.Npc[mapNPC.num];
                double distance = botPos.DistanceTo(targetPos);
                // name string
                RenderString targetName = new RenderString(npcData.Name.Trim());
                NinMods.Main.oRenderText(client.modText.Font[1], targetName.Message, xBase - targetName.Width, yOffset, SFML.Graphics.Color.Red, false, targetName.FontSize, client.modGraphics.GameWindowForm.Window);
                yOffset += (targetName.Height + 4);
                // attack radius string
                RenderString targetRange = new RenderString($"Range: {npcData.Range}");
                NinMods.Main.oRenderText(client.modText.Font[1], targetRange.Message, xBase - targetRange.Width, yOffset, SFML.Graphics.Color.Red, false, targetRange.FontSize, client.modGraphics.GameWindowForm.Window);
                yOffset += (targetRange.Height + 4);
                // distance string
                RenderString targetDistance = new RenderString($"Distance: {distance}");
                NinMods.Main.oRenderText(client.modText.Font[1], targetDistance.Message, xBase - targetDistance.Width, yOffset, SFML.Graphics.Color.Red, false, targetDistance.FontSize, client.modGraphics.GameWindowForm.Window);
                yOffset += (targetDistance.Height + 4);
                // behavior string
                RenderString targetBehavior = new RenderString($"Behavior: {npcData.Behaviour}");
                NinMods.Main.oRenderText(client.modText.Font[1], targetBehavior.Message, xBase - targetBehavior.Width, yOffset, SFML.Graphics.Color.Red, false, targetBehavior.FontSize, client.modGraphics.GameWindowForm.Window);
                yOffset += (targetBehavior.Height + 4);
                // AttackSpeed string
                RenderString targetAttackSpeed = new RenderString($"AttackSpeed: {npcData.AttackSpeed}");
                NinMods.Main.oRenderText(client.modText.Font[1], targetAttackSpeed.Message, xBase - targetAttackSpeed.Width, yOffset, SFML.Graphics.Color.Red, false, targetAttackSpeed.FontSize, client.modGraphics.GameWindowForm.Window);
                yOffset += (targetAttackSpeed.Height + 4);
                // is attacking string
                if (mapNPC.target == client.modGlobals.MyIndex)
                {
                    RenderString targetTarget = new RenderString("Is Attacking");
                    NinMods.Main.oRenderText(client.modText.Font[1], targetTarget.Message, xBase - targetTarget.Width, yOffset, SFML.Graphics.Color.Red, false, targetTarget.FontSize, client.modGraphics.GameWindowForm.Window);
                    yOffset += (targetTarget.Height + 4);
                }
                // Attacking string
                RenderString targetAttacking = new RenderString($"Attacking: {mapNPC.Attacking}");
                NinMods.Main.oRenderText(client.modText.Font[1], targetAttacking.Message, xBase - targetAttacking.Width, yOffset, SFML.Graphics.Color.Red, false, targetAttacking.FontSize, client.modGraphics.GameWindowForm.Window);
                yOffset += (targetAttacking.Height + 4);
                // Attacking string
                RenderString targetAttackTimer = new RenderString($"AttackTmr: {mapNPC.AttackTimer}");
                NinMods.Main.oRenderText(client.modText.Font[1], targetAttackTimer.Message, xBase - targetAttackTimer.Width, yOffset, SFML.Graphics.Color.Red, false, targetAttackTimer.FontSize, client.modGraphics.GameWindowForm.Window);
                yOffset += (targetAttackTimer.Height + 4);
                // current tick string
                RenderString globalTick = new RenderString($"Tick: {client.modGlobals.Tick}");
                NinMods.Main.oRenderText(client.modText.Font[1], globalTick.Message, xBase - globalTick.Width, yOffset, SFML.Graphics.Color.Red, false, globalTick.FontSize, client.modGraphics.GameWindowForm.Window);
                yOffset += (globalTick.Height + 4);
                if ((mapNPC.Attacking != 0) && ((mapNPC.AttackTimer == 0) || ((mapNPC.AttackTimer + npcData.AttackSpeed) <= client.modGlobals.Tick)))
                {
                    Logger.Log.WritePipe($"Predicted attack @ {client.modGlobals.Tick}, next attack should be @ {client.modGlobals.Tick + npcData.AttackSpeed}");
                }
            }
        }

        // the heart of the bot. this runs every tick on the game's thread.
        public static void Main_OnGameLoop_Pre()
        {
            if (Logger.Log.NeedsInit)
            {
                Logger.Log.InitPipe();
            }
            // TO-DO:
            // check exactly when game state becomes valid for us.
            // now that we're injecting at game startup rather than once we're fully loaded in-game, we **have** to make sure the game state is valid before running any of our logic
            if ((MapPathfindingGrid == null) && (client.modTypes.Map != null) && (string.IsNullOrEmpty(client.modTypes.Map.Name) == false))
            {
                MapPathfindingGrid = new SquareGrid(client.modTypes.Map, client.modTypes.Map.Tile);
            }
            try
            {
                if (GameState.SaveSnapshot() == false)
                {
                    Logger.Log.Write($"Could not save snapshot (myIndex: {client.modGlobals.MyIndex})", Logger.ELogType.Warning);
                }
                lock (_lock)
                {
                    if (NinMods.Main.frmPlayerStats == null)
                    {
                        Logger.Log.Write("Initializing player stats form");
                        NinMods.Main.frmPlayerStats = new PlayerStatsForm();
                        NinMods.Main.frmPlayerStats.Show();
                    }
                    if (NinMods.Main.frmPlayerStats.Visible)
                    {
                        client.modTypes.PlayerRec bot = Bot.BotUtils.GetSelf();
                        NinMods.Main.frmPlayerStats.UpdatePlayerStats(bot);
                    }
                }
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
                        Logger.Log.WritePipe("Catastrophic error occurred performing MoveToCursor command");
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
            // check for changed game state
            // this will also push any differences to the bot
            // TO-DO:
            // make it more clear what exactly this function does
            if (GameState.CheckNewState() == false)
            {
                Logger.Log.Write($"Could not compare post state to pre state (myIndex: {client.modGlobals.MyIndex})", Logger.ELogType.Warning);
            }
        }

        // for updating our pathfinding grid (and probably some other stuff later)
        // this *should* be the best hook for map transitions. player & game state should be fully loaded w/ new map values by time this is called.
        public static void Main_OnMapLoaded_Post(int Index, byte[] data, int StartAddr, int ExtraVar)
        {
            client.modTypes.PlayerRec bot = Bot.BotUtils.GetSelf();
            Logger.Log.WritePipe($"Loaded new map {bot.Map}", Logger.ELogType.Info, null, true);
            if ((client.modTypes.Map != null) && (string.IsNullOrEmpty(client.modTypes.Map.Name) == false))
            {
                MapPathfindingGrid = new SquareGrid(client.modTypes.Map, client.modTypes.Map.Tile);
            }
            else
            {
                Logger.Log.WritePipe($"Not loading map into pathfinding grid because it's invalid (map: {client.modTypes.Map}, name: {(client.modTypes.Map == null ? "<invalid>" : client.modTypes.Map.Name)})", Logger.ELogType.Error);
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
            //Logger.Log.WritePipe($"Saw combat msg {text} with color {tColor}");
        }

        public static void Main_OnActionMsg_Post(int Index, byte[] data, int StartAddr, int ExtraVar)
        {
            client.clsBuffer buffer = new client.clsBuffer(data);
            string message = buffer.ReadString();
            int color = buffer.ReadLong();
            int msgType = buffer.ReadLong();
            Logger.Log.Write($"Saw action msg '{message}' (color: {color} type: {msgType})");
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
            NinMods.Main.oRenderText(client.modText.Font[1], $"ScrWidth: {client.modGraphics.ScreenWidth}", 10, 10, SFML.Graphics.Color.Red, false, 13, client.modGraphics.GameWindowForm.Window);
            NinMods.Main.oRenderText(client.modText.Font[1], $"ScrHeight: {client.modGraphics.ScreenHeight}", 10, 24, SFML.Graphics.Color.Red, false, 13, client.modGraphics.GameWindowForm.Window);
            Utils.DrawNetStats();
            CheckForAttackingMobs();
            DrawTargetInfo();
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

            client.clsBuffer clsBuffer2 = new client.clsBuffer(data);
            int num = clsBuffer2.ReadLong();
            client.modEnumerations.ServerPackets packetID = (client.modEnumerations.ServerPackets)num;
            Logger.Log.WriteNetLog($"RECV packet {packetID} (ID: {num})");
            // NOTE:
            // special handling for auto-login process.
            // instead of hooking client.modAuth.Auth_HandleServerDetails we'll just check for that packet here
            // it is important that client.modAuth.Auth_HandleServerDetails is called before we reach this point, though.
            // we ensure that's the case w/ the CallOriginalFunction above.
            /*
            if (packetID == client.modEnumerations.ServerPackets.Auth_ServerDetails)
            {
                FinishAutoLogin();
            }
            */
        }

        // for logging packets that we send to the server
        public static void Main_OnNetSend_Post(byte[] data, bool auth)
        {
            NinMods.Main.NetBytesSent += data.Length;

            client.clsBuffer clsBuffer2 = new client.clsBuffer(data);
            int num = clsBuffer2.ReadLong();
            client.modEnumerations.ClientPackets packetID = (client.modEnumerations.ClientPackets)num;
            Logger.Log.WriteNetLog($"SENT packet {packetID} (ID: {num})");
        }
    }
}