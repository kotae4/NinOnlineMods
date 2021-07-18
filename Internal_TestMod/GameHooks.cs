using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NinMods.Hooking;
using NinMods.Utilities;
using System.Reflection;
using NinMods.Logging;

namespace NinMods
{
    public static class GameHooks
    {
        public enum EEventType
        {
            OnKeyPress,
            OnSetPlayerAccess,
            OnMenuLoop,
            OnGameLoop,
            OnMapLoaded,
            OnCombatMsg,
            OnNetRecv,
            OnNetSend,
            OnDraw_Worldspace,
            OnDraw_Screenspace
        }
        // i really hate this design
        // i wish i could wrap them into a GameHookData class
        #region Hook fields
        public static GenericGameHookClass_Void<SFML.Window.Keyboard.Key> HandleKeyPressesHook = new GenericGameHookClass_Void<SFML.Window.Keyboard.Key>(
            typeof(client.modInput), "HandleKeyPresses", typeof(NinMods.GameHooks), "hk_modInput_HandleKeyPresses");

        public static GenericGameHookClass_Void<int, int> SetPlayerAccessHook = new GenericGameHookClass_Void<int, int>(
            typeof(client.modDatabase), "SetPlayerAccess", typeof(NinMods.GameHooks), "hk_modDatabase_SetPlayerAccess");

        public static GenericGameHookClass_Void MenuLoopHook = new GenericGameHookClass_Void(
            typeof(client.modGameLogic), "MenuLoop", typeof(NinMods.GameHooks), "hk_modGameLogic_MenuLoop");

        public static GenericGameHookClass_Void GameLoopHook = new GenericGameHookClass_Void(
            typeof(client.modGameLogic), "GameLoop", typeof(NinMods.GameHooks), "hk_modGameLogic_GameLoop");

        // for making sure we always have the latest tiledata for pathfinding
        public delegate void dHandleMapDone(int Index, byte[] data, int StartAddr, int ExtraVar);
        public static GenericGameHookClass_Void<int, byte[], int, int> HandleMapDoneHook = new GenericGameHookClass_Void<int, byte[], int, int>(
            typeof(client.modHandleData), "HandleMapDone", typeof(NinMods.GameHooks), "hk_modHandleData_HandleMapDone");

        // for damage log / spell accuracy
        public static GenericGameHookClass_Void<int, byte[], int, int> HandleCombatMsgHook = new GenericGameHookClass_Void<int, byte[], int, int>(
            typeof(client.modHandleData), "HandleCombatMsg", typeof(NinMods.GameHooks), "hk_modHandleData_HandleCombatMsg");

        // for logging incoming network messages (this is what receives all packets and then dispatches them to the specific packet handlers)
        public static GenericGameHookClass_Void<byte[]> HandleDataHook = new GenericGameHookClass_Void<byte[]>(
            typeof(client.modHandleData), "HandleData", typeof(NinMods.GameHooks), "hk_modHandleData_HandleData");

        // for logging outgoing network messages
        public static GenericGameHookClass_Void<byte[], bool> SendDataHook = new GenericGameHookClass_Void<byte[], bool>(
            typeof(client.modClientTCP), "SendData", typeof(NinMods.GameHooks), "hk_modClientTCP_SendData");

        // just for tile overlays. can remove later, probably.
        public static GenericGameHookClass_Void DrawWeatherHook = new GenericGameHookClass_Void(
            typeof(client.modGraphics), "DrawWeather", typeof(NinMods.GameHooks), "hk_modGraphics_DrawWeather");

        // for HUD overlays
        public delegate void dDrawGUI();
        public static GenericGameHookClass_Void DrawGUIHook = new GenericGameHookClass_Void(
            typeof(client.modGraphics), "DrawGUI", typeof(NinMods.GameHooks), "hk_modGraphics_DrawGUI");
        #endregion

        public static Dictionary<EEventType, HookData> _Hooks = new Dictionary<EEventType, HookData>()
        {
            { EEventType.OnKeyPress, HandleKeyPressesHook },
            { EEventType.OnSetPlayerAccess, SetPlayerAccessHook },
            { EEventType.OnDraw_Worldspace, DrawWeatherHook },
            { EEventType.OnNetRecv, HandleDataHook },
            { EEventType.OnNetSend, SendDataHook },
            { EEventType.OnDraw_Screenspace, DrawGUIHook },
            { EEventType.OnMapLoaded, HandleMapDoneHook },
            //{ EEventType.OnCombatMsg, HandleCombatMsgHook },
            { EEventType.OnMenuLoop, MenuLoopHook },
            { EEventType.OnGameLoop, GameLoopHook }
        };

        public static int numSuccessfullyHooked = 0;

        public static void SetupManagedHookerHooks()
        {
            try
            {
                // the methods have to be JIT'd to native code before we can hook them
                // normally, we'd use System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle) to force JIT compilation
                // but for some reason (probably due to their method body encryption) this throws an exception
                // so we just call the method ourselves to force the JIT compilation.
                // we also set some state to ensure the method early-exits. we don't actually want the method to do any work.

                // the try-catch copypasta is unfortunately necessary.
                // ensuring JIT compilation is absolutely vital, and some of these method calls will absolutely throw an exception.
                // an exception is fine, the method is JIT'd whether the call succeeds or not.
                // and we want to call *every* method here, so we can't just use one big try-catch block. they have to be individually wrapped.
                try
                {
                    client.modGlobals.InMapEditor = true;
                    client.modInput.HandleKeyPresses(SFML.Window.Keyboard.Key.W);
                    client.modGlobals.InMapEditor = false;
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException(ex);
                    Logger.Log.Write("The above exception is probably safe!");
                }
                try
                {
                    client.modDatabase.SetPlayerAccess(-1, 1);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException(ex);
                    Logger.Log.Write("The above exception is probably safe!");
                }
                try
                {
                    client.modGraphics.DrawWeather();
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException(ex);
                    Logger.Log.Write("The above exception is probably safe!");
                }

                try
                {
                    client.clsBuffer dummyPacket = new client.clsBuffer();
                    // packet IDs of 0 are ignored.
                    dummyPacket.WriteLong(0);
                    byte[] dummyPacketBuf = dummyPacket.ToArray();
                    client.modHandleData.HandleData(dummyPacketBuf);
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException(ex);
                    Logger.Log.Write("The above exception is probably safe!");
                }

                try
                {
                    System.Reflection.MethodInfo drawGUIMethodInfo = typeof(client.modGraphics).GetMethod("DrawGUI", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (drawGUIMethodInfo == null)
                    {
                        Logger.Log.WriteError("Could not get DrawGUI methodinfo");
                    }
                    else
                    {
                        NinMods.GameHooks.dDrawGUI oDrawGUI = (NinMods.GameHooks.dDrawGUI)drawGUIMethodInfo.CreateDelegate(typeof(NinMods.GameHooks.dDrawGUI));
                        oDrawGUI();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException(ex);
                    Logger.Log.Write("The above exception is probably safe!");
                }

                try
                {
                    System.Reflection.MethodInfo handleMapDoneMethodInfo = typeof(client.modHandleData).GetMethod("HandleMapDone", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (handleMapDoneMethodInfo == null)
                    {
                        Logger.Log.WriteError("Could not get HandleMapDone methodinfo");
                    }
                    else
                    {

                        NinMods.GameHooks.dHandleMapDone oHandleMapDone = (NinMods.GameHooks.dHandleMapDone)handleMapDoneMethodInfo.CreateDelegate(typeof(NinMods.GameHooks.dHandleMapDone));
                        oHandleMapDone(0, null, 0, 0);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException(ex);
                    Logger.Log.Write("The above exception is probably safe!");
                }

                // NOTE:
                // no way to force an early exit here. hopefully doesn't cause a packet to be missed.
                try
                {
                    client.modGameLogic.GameLoop();
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException(ex);
                    Logger.Log.Write("The above exception is probably safe!");
                }

                // WARNING:
                // no way to force the JIT compilation of these methods..
                // sendDataHook, drawGUIHook (partially), handleMapDoneHook (partially), loadMapHook, handleCombatMsgHook
            }
            catch (Exception ex)
            {
                Logger.Log.WriteException(ex);
                Logger.Log.Write("The above exception is probably safe!");
            }

            // now we just loop through all our hooks and try hooking... we expect some to fail, we'll keep trying to hook those from GameLoop().
            foreach (KeyValuePair<EEventType, HookData> hook in _Hooks)
            {
                // skip these, they're weird...
                if ((hook.Key == EEventType.OnMapLoaded) || (hook.Key == EEventType.OnCombatMsg)) continue;
                // per-iteration try-catch block because we expect some to fail, but still want to try to hook as many as possible
                try
                {
                    // ignoring the return value, eh...
                    hook.Value.TryHook();
                }
                catch (Exception ex)
                {
                    Logger.Log.Alert($"exception '{ex.GetType().Name}' occurred: {ex.Message}\n\n{ex.StackTrace}", NinMods.Main.MAIN_CAPTION, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, Logger.ELogType.Error, null, true);
                    return;
                }
            }

            if ((NinMods.GameHooks.HandleKeyPressesHook.IsHooked() == false) || (NinMods.GameHooks.SetPlayerAccessHook.IsHooked() == false) || (NinMods.GameHooks.MenuLoopHook.IsHooked() == false) || (NinMods.GameHooks.GameLoopHook.IsHooked() == false) || (NinMods.GameHooks.DrawWeatherHook.IsHooked() == false))
            {
                Logger.Log.Alert("Could not install hooks for unknown reason", NinMods.Main.MAIN_CAPTION, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, Logger.ELogType.Info, null, true);
            }
            else
            {
                Logger.Log.Write("Success!", Logger.ELogType.Info, null, true);
            }
        }

        // for methods that can't be forced to JIT compile
        // we have to continuously check if the method exists, and, if so, finally hook it.
        public static void AttemptRehooking()
        {
            NinMods.GameHooks.numSuccessfullyHooked = 0;
            // just loop through all our hooks and try hooking...
            foreach (KeyValuePair<EEventType, HookData> hook in _Hooks)
            {
                if (hook.Value.IsHooked())
                {
                    NinMods.GameHooks.numSuccessfullyHooked += 1;
                    continue;
                }
                // per-iteration try-catch block because we expect some to fail, but still want to try to hook as many as possible
                try
                {
                    // ignoring the return value, eh...
                    hook.Value.TryHook();
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteError($"Exception '{ex.GetType().Name}' occurred: {ex.Message}\n\n{ex.StackTrace}");
                    return;
                }
            }
        }

        public static void RegisterEventHandler(EEventType eventName, EHookExecutionState state, Type handlerType, string handlerMethodName)
        {
            MethodInfo handlerMethod = Hooking.Utilities.Utility.GetMethodByName(handlerType, handlerMethodName);
            if (handlerMethod == null)
            {
                Logger.Log.WriteError($"Could not register handler {handlerType.Name}::{handlerMethodName} (could not find methodinfo)");
                return;
            }
            HookData hook = null;
            if (_Hooks.TryGetValue(eventName, out hook))
            {
                EventInfo hookEventInfo = null;
                if (state == EHookExecutionState.Pre)
                {
                    hookEventInfo = hook.GetType().GetEvent("Pre");
                }
                else
                {
                    hookEventInfo = hook.GetType().GetEvent("Post");
                }
                if (hookEventInfo == null)
                {
                    Logger.Log.WriteError($"Could not register handler {handlerType.Name}::{handlerMethodName} (could not find hook event)");
                    return;
                }
                Type hookEventHandlerType = hookEventInfo.EventHandlerType;

                Delegate handlerDelegate = Delegate.CreateDelegate(hookEventHandlerType, null, handlerMethod);

                MethodInfo hookEventAddMethod = hookEventInfo.GetAddMethod();
                Object[] addHandlerArgs = { handlerDelegate };
                hookEventAddMethod.Invoke(hook, addHandlerArgs);
            }
            else
            {
                Logger.Log.WriteError($"Could not register handler for non-existent event '{eventName}'");
            }
        }

        // for auto-login
        public static void hk_modGameLogic_MenuLoop()
        {
            if (NinMods.GameHooks.MenuLoopHook.FirstRun == true)
            {
                NinMods.GameHooks.MenuLoopHook.FirstRun = false;
                Logger.Log.Write("Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            NinMods.GameHooks.MenuLoopHook.FireEvent(EHookExecutionState.Pre);
            NinMods.GameHooks.MenuLoopHook.Hook.CallOriginalFunction(typeof(void));
            NinMods.GameHooks.MenuLoopHook.FireEvent(EHookExecutionState.Post);
        }

        // the heart of the bot. this runs every tick on the game's thread.
        public static void hk_modGameLogic_GameLoop()
        {
            if (NinMods.GameHooks.GameLoopHook.FirstRun == true)
            {
                NinMods.GameHooks.GameLoopHook.FirstRun = false;
                Logger.Log.Write("Successfully hooked!", Logger.ELogType.Info, null, true);
            }
            // this is the only hook that has logic within the hook itself, everything else should be done on event handlers.
            if (NinMods.Main.HasInitialized == false)
            {
                // early exit
                NinMods.GameHooks.GameLoopHook.Hook.CallOriginalFunction(typeof(void));
                return;
            }

            if (NinMods.GameHooks.numSuccessfullyHooked < NinMods.GameHooks._Hooks.Count)
                NinMods.GameHooks.AttemptRehooking();


            // call original
            NinMods.GameHooks.GameLoopHook.FireEvent(EHookExecutionState.Pre);
            NinMods.GameHooks.GameLoopHook.Hook.CallOriginalFunction(typeof(void));
            NinMods.GameHooks.GameLoopHook.FireEvent(EHookExecutionState.Post);
        }

        // for updating our pathfinding grid (and probably some other stuff later)
        // this *should* be the best hook for map transitions. player & game state should be fully loaded w/ new map values by time this is called.
        public static void hk_modHandleData_HandleMapDone(int Index, byte[] data, int StartAddr, int ExtraVar)
        {
            if (NinMods.GameHooks.HandleMapDoneHook.FirstRun == true)
            {
                NinMods.GameHooks.HandleMapDoneHook.FirstRun = false;
                Logger.Log.Write("Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            // call original
            NinMods.GameHooks.HandleMapDoneHook.FireEvent(Index, data, StartAddr, ExtraVar, EHookExecutionState.Pre);
            NinMods.GameHooks.HandleMapDoneHook.Hook.CallOriginalFunction(typeof(void), Index, data, StartAddr, ExtraVar);
            NinMods.GameHooks.HandleMapDoneHook.FireEvent(Index, data, StartAddr, ExtraVar, EHookExecutionState.Post);
        }

        // for damage log / spell accuracy
        public static void hk_modHandleData_HandleCombatMsg(int Index, byte[] data, int StartAddr, int ExtraVar)
        {
            if (NinMods.GameHooks.HandleCombatMsgHook.FirstRun == true)
            {
                NinMods.GameHooks.HandleCombatMsgHook.FirstRun = false;
                Logger.Log.Write("Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            NinMods.GameHooks.HandleCombatMsgHook.FireEvent(Index, data, StartAddr, ExtraVar, EHookExecutionState.Pre);
            NinMods.GameHooks.HandleCombatMsgHook.Hook.CallOriginalFunction(typeof(void), Index, data, StartAddr, ExtraVar);
            NinMods.GameHooks.HandleCombatMsgHook.FireEvent(Index, data, StartAddr, ExtraVar, EHookExecutionState.Post);
        }

        // for drawing tile overlays
        // NOTE:
        // everything drawn here is in world-space (coords are tile coords)
        public static void hk_modGraphics_DrawWeather()
        {
            if (NinMods.GameHooks.DrawWeatherHook.FirstRun == true)
            {
                NinMods.GameHooks.DrawWeatherHook.FirstRun = false;
                Logger.Log.Write("Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            NinMods.GameHooks.DrawWeatherHook.FireEvent(EHookExecutionState.Pre);
            NinMods.GameHooks.DrawWeatherHook.Hook.CallOriginalFunction(typeof(void));
            NinMods.GameHooks.DrawWeatherHook.FireEvent(EHookExecutionState.Post);
        }

        // for drawing on the screen
        // NOTE:
        // everything drawn here is in screen-space
        public static void hk_modGraphics_DrawGUI()
        {
            if (NinMods.GameHooks.DrawGUIHook.FirstRun == true)
            {
                NinMods.GameHooks.DrawGUIHook.FirstRun = false;
                Logger.Log.Write("Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            NinMods.GameHooks.DrawGUIHook.FireEvent(EHookExecutionState.Pre);
            NinMods.GameHooks.DrawGUIHook.Hook.CallOriginalFunction(typeof(void));
            NinMods.GameHooks.DrawGUIHook.FireEvent(EHookExecutionState.Post);
        }

        // for our keybinds :)
        public static void hk_modInput_HandleKeyPresses(SFML.Window.Keyboard.Key keyAscii)
        {
            if (NinMods.GameHooks.HandleKeyPressesHook.FirstRun == true)
            {
                NinMods.GameHooks.HandleKeyPressesHook.FirstRun = false;
                Logger.Log.Write("Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            NinMods.GameHooks.HandleKeyPressesHook.FireEvent(keyAscii, EHookExecutionState.Pre);
            NinMods.GameHooks.HandleKeyPressesHook.Hook.CallOriginalFunction(typeof(void), keyAscii);
            NinMods.GameHooks.HandleKeyPressesHook.FireEvent(keyAscii, EHookExecutionState.Post);
        }

        // for detecting when admins / GMs enter the map
        public static void hk_modDatabase_SetPlayerAccess(int index, int access)
        {
            if (NinMods.GameHooks.SetPlayerAccessHook.FirstRun == true)
            {
                NinMods.GameHooks.SetPlayerAccessHook.FirstRun = false;
                Logger.Log.Write("Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            NinMods.GameHooks.SetPlayerAccessHook.FireEvent(index, access, EHookExecutionState.Pre);
            NinMods.GameHooks.SetPlayerAccessHook.Hook.CallOriginalFunction(typeof(void), index, access);
            NinMods.GameHooks.SetPlayerAccessHook.FireEvent(index, access, EHookExecutionState.Post);
        }

        // for logging packets that we receive from the server (and for notifying the bot of certain ones)
        public static void hk_modHandleData_HandleData(byte[] data)
        {
            if (NinMods.GameHooks.HandleDataHook.FirstRun == true)
            {
                NinMods.GameHooks.HandleDataHook.FirstRun = false;
                Logger.Log.Write("Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            NinMods.GameHooks.HandleDataHook.FireEvent(data, EHookExecutionState.Pre);
            NinMods.GameHooks.HandleDataHook.Hook.CallOriginalFunction(typeof(void), data);
            NinMods.GameHooks.HandleDataHook.FireEvent(data, EHookExecutionState.Post);
        }

        // for logging packets that we send to the server
        public static void hk_modClientTCP_SendData(byte[] data, bool auth)
        {
            if (NinMods.GameHooks.SendDataHook.FirstRun == true)
            {
                NinMods.GameHooks.SendDataHook.FirstRun = false;
                Logger.Log.Write("Successfully hooked!", Logger.ELogType.Info, null, true);
            }

            NinMods.GameHooks.SendDataHook.FireEvent(data, auth, EHookExecutionState.Pre);
            NinMods.GameHooks.SendDataHook.Hook.CallOriginalFunction(typeof(void), data, auth);
            NinMods.GameHooks.SendDataHook.FireEvent(data, auth, EHookExecutionState.Post);
        }
    }
}
