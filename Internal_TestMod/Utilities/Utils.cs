using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NinMods.Hooking;

namespace NinMods.Utilities
{
    public static class Utils
    {
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
                NinMods.Main.handleKeyPressesHook = ManagedHooker.HookMethod<NinMods.Main.dOpenHandleKeyPresses>(typeof(client.modInput), "HandleKeyPresses", NinMods.Main.hk_modInput_HandleKeyPresses, 0);

                client.modDatabase.SetPlayerAccess(-1, 1);
                NinMods.Main.setPlayerAcessHook = ManagedHooker.HookMethod<NinMods.Main.dOpenSetPlayerAccess>(typeof(client.modDatabase), "SetPlayerAccess", NinMods.Main.hk_modDatabase_SetPlayerAccess, 0);

                // NOTE:
                // no way to force an early exit here. hopefully doesn't cause a packet to be missed.
                client.modGameLogic.GameLoop();
                NinMods.Main.gameLoopHook = ManagedHooker.HookMethod<NinMods.Main.dOpenGameLoop>(typeof(client.modGameLogic), "GameLoop", NinMods.Main.hk_modGameLogic_GameLoop, 0);

                client.modGraphics.DrawWeather();
                NinMods.Main.drawWeatherHook = ManagedHooker.HookMethod<NinMods.Main.dDrawWeather>(typeof(client.modGraphics), "DrawWeather", NinMods.Main.hk_modGraphics_DrawWeather, 0);
                // WARNING:
                // no way to force the JIT compilation of these two methods..
                try
                {
                    System.Reflection.MethodInfo methodInfo = typeof(client.modGraphics).GetMethod("DrawGUI", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (methodInfo == null)
                    {
                        Logger.Log.WriteError("NinMods.Main", "SetupManagedHookerHooks", "Could not get DrawGUI methodinfo");
                    }
                    else
                    {
                        NinMods.Main.dDrawGUI oDrawGUI = (NinMods.Main.dDrawGUI)methodInfo.CreateDelegate(typeof(NinMods.Main.dDrawGUI));
                        oDrawGUI();
                        NinMods.Main.drawGUIHook = ManagedHooker.HookMethod<NinMods.Main.dDrawGUI>(typeof(client.modGraphics), "DrawGUI", NinMods.Main.hk_modGraphics_DrawGUI, 0);
                    }

                    // NOTE: this hook is unstable.
                    //handleMapDataHook = ManagedHooker.HookMethod<dHandleMapData>(typeof(client.modHandleData), "HandleMapData", hk_modHandleData_HandleMapData, 0);
                    NinMods.Main.loadMapHook = ManagedHooker.HookMethod<NinMods.Main.dLoadMap>(typeof(client.modDatabase), "LoadMap", NinMods.Main.hk_modDatabase_LoadMap, 0);

                    NinMods.Main.handleDataHook = ManagedHooker.HookMethod<NinMods.Main.dHandleData>(typeof(client.modHandleData), "HandleData", NinMods.Main.hk_modHandleData_HandleData, 0);
                    NinMods.Main.sendDataHook = ManagedHooker.HookMethod<NinMods.Main.dSendData>(typeof(client.modClientTCP), "SendData", NinMods.Main.hk_modClientTCP_SendData, 0);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("NinMods.Main", "SetupManagedHookerHooks", ex);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Alert("NinMods.Main", "SetupManagedHookerHooks", "exception '" + ex.GetType().Name + "' occurred: " + ex.Message + "\n\n" + ex.StackTrace, NinMods.Main.MAIN_CAPTION, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, Logger.ELogType.Error, null, true);
                return;
            }
            if ((NinMods.Main.handleKeyPressesHook == null) || (NinMods.Main.setPlayerAcessHook == null) || (NinMods.Main.gameLoopHook == null) || (NinMods.Main.drawWeatherHook == null))
            {
                Logger.Log.Alert("NinMods.Main", "SetupManagedHookerHooks", "Could not install hooks for unknown reason", NinMods.Main.MAIN_CAPTION, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, Logger.ELogType.Info, null, true);
            }
            else
            {
                Logger.Log.Write("NinMods.Main", "SetupManagedHookerHooks", "Success!", Logger.ELogType.Info, null, true);
            }
        }

        // for methods that can't be forced to JIT compile
        // we have to continuously check if the method exists, and, if so, finally hook it.
        public static void AttemptRehooking()
        {
            if (NinMods.Main.loadMapHook == null)
            {
                try
                {
                    NinMods.Main.loadMapHook = ManagedHooker.HookMethod<NinMods.Main.dLoadMap>(typeof(client.modDatabase), "LoadMap", NinMods.Main.hk_modDatabase_LoadMap, 0);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("NinMods.Main", "AttemptRehooking", ex);
                }
            }
            if (NinMods.Main.drawGUIHook == null)
            {
                try
                {
                    NinMods.Main.drawGUIHook = ManagedHooker.HookMethod<NinMods.Main.dDrawGUI>(typeof(client.modGraphics), "DrawGUI", NinMods.Main.hk_modGraphics_DrawGUI, 0);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("NinMods.Main", "AttemptRehooking", ex);
                }
            }
            // NOTE: this hook is unstable.
            /*
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
            */
            if (NinMods.Main.handleDataHook == null)
            {
                try
                {
                    NinMods.Main.handleDataHook = ManagedHooker.HookMethod<NinMods.Main.dHandleData>(typeof(client.modHandleData), "HandleData", NinMods.Main.hk_modHandleData_HandleData, 0);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("NinMods.Main", "AttemptRehooking", ex);
                }
            }
            if (NinMods.Main.sendDataHook == null)
            {
                try
                {
                    NinMods.Main.sendDataHook = ManagedHooker.HookMethod<NinMods.Main.dSendData>(typeof(client.modClientTCP), "SendData", NinMods.Main.hk_modClientTCP_SendData, 0);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("NinMods.Main", "AttemptRehooking", ex);
                }
            }
        }

        public static void DrawTileTypeOverlay()
        {
            if (NinMods.Main.oRenderText == null) return;
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

        public static void DrawNetStats()
        {
            // NOTE:
            // game draws FPS at ScreenHeight - 26, then Ping at -14 from that.
            // so i assume add/subtract 14 for pretty line spacing.
            int num = client.modGraphics.ScreenHeight - 82;
            NinMods.Main.oRenderText(client.modText.Font[1], $"NetSent: {NinMods.Main.NetBytesSent}\nNetRecv: {NinMods.Main.NetBytesReceived}\nPing: {client.modGlobals.Ping}", 9, num, SFML.Graphics.Color.Red, false, 13, client.modGraphics.GameWindowForm.Window);
        }

        public static void DumpMapData()
        {
            client.modTypes.PlayerRec bot = NinMods.Bot.BotUtils.GetSelf();
            client.modTypes.MapRec map = client.modTypes.Map;
            Logger.Log.Write("NinMods.Main", "DumpMapData", $"\n===== Dumping map ({bot.Map}) =====");
            try
            {
                if (System.IO.Directory.Exists("GAME_DUMP") == false)
                    System.IO.Directory.CreateDirectory("GAME_DUMP");
                if (System.IO.Directory.Exists("GAME_DUMP\\Warps") == false)
                    System.IO.Directory.CreateDirectory("GAME_DUMP\\Warps");

                string safeMapName = bot.Map.ToString() + "_" + map.Name;
                foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
                    safeMapName = safeMapName.Replace(invalidChar, '-');
                // there's no real reason for these to be nested
                using (System.IO.FileStream fsFullDump = System.IO.File.Open("GAME_DUMP\\" + safeMapName + ".fdmp", System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read))
                {
                    using (System.IO.FileStream fsWarpDump = System.IO.File.Open("GAME_DUMP\\Warps\\" + safeMapName + ".wdmp", System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read))
                    {
                        using (System.IO.StreamWriter swFullDump = new System.IO.StreamWriter(fsFullDump))
                        {
                            using (System.IO.StreamWriter swWarpDump = new System.IO.StreamWriter(fsWarpDump))
                            {
                                for (int tock = 0; tock < 2; tock++)
                                {
                                    System.IO.StreamWriter sw = null;
                                    if (tock == 0)
                                        sw = swFullDump;
                                    else
                                        sw = swWarpDump;
                                    // dump flat fields
                                    sw.WriteLine($"Name: {map.Name}");
                                    sw.WriteLine($"ID: {bot.Map}");
                                    sw.WriteLine($"Revision: {map.Revision}");
                                    sw.WriteLine($"Secret: {map.Secret}");
                                    sw.WriteLine($"Indoor: {map.Indoor}");
                                    sw.WriteLine($"CurrentEvents: {map.CurrentEvents}");
                                    sw.WriteLine($"eventcount: {map.eventcount}");
                                    // map boundary warps (this is responsible for most map transitions)
                                    sw.WriteLine($"LeftWarp: {map.Left}");
                                    sw.WriteLine($"RightWarp: {map.Right}");
                                    sw.WriteLine($"UpWarp: {map.Up}");
                                    sw.WriteLine($"DownWarp: {map.Down}");
                                    // dump tile array / tile data
                                    int tileLengthX = map.Tile.GetLength(0);
                                    int tileLengthY = map.Tile.GetLength(1);
                                    sw.WriteLine($"tileLengthX: {tileLengthX} (map.MaxX {map.MaxX})");
                                    sw.WriteLine($"tileLengthX: {tileLengthY} (map.MaxY {map.MaxY})");
                                    sw.WriteLine("=== TileData ===");
                                    for (int tileX = 0; tileX < tileLengthX; tileX++)
                                    {
                                        for (int tileY = 0; tileY < tileLengthY; tileY++)
                                        {
                                            client.modTypes.TileRec tile = map.Tile[tileX, tileY];
                                            NinMods.Utilities.GameUtils.ETileType tileType = (NinMods.Utilities.GameUtils.ETileType)tile.Type;
                                            if ((tileType != Utilities.GameUtils.ETileType.TILE_TYPE_WARP) && (tock != 0))
                                                continue;
                                            sw.WriteLine($"Tile[{tileX}, {tileY}].Type: {tileType}");
                                            sw.WriteLine($"Tile[{tileX}, {tileY}].Data1: {tile.Data1}");
                                            sw.WriteLine($"Tile[{tileX}, {tileY}].Data2: {tile.Data2}");
                                            sw.WriteLine($"Tile[{tileX}, {tileY}].Data3: {tile.Data3}");
                                            sw.WriteLine($"Tile[{tileX}, {tileY}].Data4: {(string.IsNullOrEmpty(tile.Data4) ? "<null>" : tile.Data4)}");
                                        }
                                    }
                                    sw.WriteLine("===== Done =====\n");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.WriteException("NinMods.Main", "DumpMapData", ex);
            }
            Logger.Log.Write("NinMods.Main", "DumpMapData", "===== Done dumping map =====\n");
        }
    }
}
