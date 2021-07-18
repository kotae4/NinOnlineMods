using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Collections.Concurrent;
using Launcher.Logging;

namespace Launcher
{
    public partial class Main
    {
        PipeServer pipe;
        bool isPipeInitialized = false;

        // kinda duplicating some of Logger's functionality here...
        private readonly System.Globalization.CultureInfo DateTimeCultureInfo_German = System.Globalization.CultureInfo.CreateSpecificCulture("de-DE");

        public bool StartPipeServer()
        {
            pipe = new PipeServer();

            pipe.ClientConnectedEvent += Pipe_OnClientConnected;
            pipe.ClientDisconnectedEvent += Pipe_OnClientDisconnected;
            pipe.MessageReceivedEvent += Pipe_OnMessageReceived;
            pipe.ErrorEvent += Pipe_OnError;
            pipe.InternalEvent += Pipe_OnInternalMessage;

            return pipe.Start();
        }

        public void ShutdownPipeServer()
        {
            pipe.ClientConnectedEvent -= Pipe_OnClientConnected;
            pipe.ClientDisconnectedEvent -= Pipe_OnClientDisconnected;
            pipe.MessageReceivedEvent -= Pipe_OnMessageReceived;
            pipe.ErrorEvent -= Pipe_OnError;
            pipe.InternalEvent -= Pipe_OnInternalMessage;

            pipe.ShutdownAllServers();
        }

        private void Pipe_OnClientConnected(object sender, ClientConnectedEventArgs eventArgs)
        {
            isPipeInitialized = true;
            Logger.Log.Write($"Pipe client#{eventArgs.UUID} connected", Logger.ELogType.Info, rtxtLog);
        }

        private void Pipe_OnClientDisconnected(object sender, ClientDisconnectedEventArgs eventArgs)
        {
            isPipeInitialized = false;
            Logger.Log.Write($"Pipe client#{eventArgs.UUID} disconnected", Logger.ELogType.Info, rtxtLog);
        }

        private void Pipe_OnMessageReceived(object sender, MessageReceivedEventArgs eventArgs)
        {
            string logString = $"[{DateTime.Now.ToString("G", DateTimeCultureInfo_German)}][Pipe#{eventArgs.UUID}]: {eventArgs.Message}";
            rtxtLog.AppendText(logString);
            // don't want to add to the injector's log file
            //Logger.Log.Write($"Received '{eventArgs.Message}' from pipe server", Logger.ELogType.Info, rtxtLog);
        }

        private void Pipe_OnError(object sender, Logging.ErrorEventArgs eventArgs)
        {
            Logger.Log.Write($"Pipe error occurred  on pipe#{eventArgs.UUID} '{eventArgs.Message}'\n{eventArgs.StackTrace}", Logger.ELogType.Error, rtxtLog);
        }

        private void Pipe_OnInternalMessage(object sender, Logging.InternalMessageEventArgs eventArgs)
        {
            Logger.Log.Write(eventArgs.Message, Logger.ELogType.Info, rtxtLog);
        }
    }
}
