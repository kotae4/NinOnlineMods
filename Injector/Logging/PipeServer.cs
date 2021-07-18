// credit: Ifat Chitin Morrison [https://www.codeproject.com/articles/864679/creating-a-server-using-named-pipes]
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace Launcher.Logging
{
    public class ClientConnectedEventArgs : EventArgs
    {
        public string UUID { get; set; }
    }

    public class ClientDisconnectedEventArgs : EventArgs
    {
        public string UUID { get; set; }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public string UUID { get; set; }
        public string Message { get; set; }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string UUID { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }

    public class InternalMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    public class PipeServer
    {
        const string PIPE_NAME = "ninbot";
        const int READ_BUFFER_SIZE = 8192;
        const int SEND_BUFFER_SIZE = 8192;

        class NamedPipeServerObject
        {
            public NamedPipeServerStream connection;
            public string ID = "";
            public byte[] recvBuf;

            public bool IsStopping = false;

            public StringBuilder MessageString;

            public NamedPipeServerObject(string pipeName, string uuid)
            {
                connection = new NamedPipeServerStream(pipeName, PipeDirection.InOut, System.IO.Pipes.NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, READ_BUFFER_SIZE, SEND_BUFFER_SIZE);
                ID = uuid;
                recvBuf = new byte[READ_BUFFER_SIZE];
                MessageString = new StringBuilder("");
            }
        }

        ConcurrentDictionary<string, NamedPipeServerObject> _servers;
        private object threadLock = 0;

        // for firing events in thread-safe way
        private readonly SynchronizationContext _synchronizationContext;

        public event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;
        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;
        public event EventHandler<InternalMessageEventArgs> InternalEvent;
        public event EventHandler<ErrorEventArgs> ErrorEvent;

        // WARNING:
        // this *must* be instantiated on the main thread!
        public PipeServer()
        {
            // saves the context of this thread so that when we later post to it from any thread, we will be posting to this thread
            // (used for firing events)
            _synchronizationContext = AsyncOperationManager.SynchronizationContext;
        }

        public bool Start()
        {
            NamedPipeServerObject firstPipe = new NamedPipeServerObject(PIPE_NAME, Guid.NewGuid().ToString());
            OnInternalMessage($"Instantiated first pipe w/ ID#{firstPipe.ID}");
            _servers = new ConcurrentDictionary<string, NamedPipeServerObject>();
            _servers[firstPipe.ID] = firstPipe;
            firstPipe.connection.BeginWaitForConnection(OnPipe_ClientConnected, firstPipe);
            return true;
        }

        public void CloseServer(string serverID)
        {
            NamedPipeServerObject serverObject;
            if (_servers.TryRemove(serverID, out serverObject) == false)
            {
                OnError(serverID, $"Failed to remove client#{serverID} from dictionary (doesn't exist?)", "");
                //Logger.Log.Alert($"[Internal] Failed to remove client#{serverID} from dictionary\n", Main.MESSAGEBOX_CAPTION);
                return;
            }
            serverObject.IsStopping = true;
            try
            {
                if (serverObject.connection.IsConnected)
                    serverObject.connection.Disconnect();
            }
            catch (Exception ex)
            {
                OnError(serverID, ex.Message, ex.StackTrace);
                //Logger.Log.Alert($"[Internal] Failed to disconnect client#{serverID}\n", Main.MESSAGEBOX_CAPTION);
            }
            finally
            {
                serverObject.connection.Close();
                serverObject.connection.Dispose();
                OnInternalMessage($"Successfully closed connection#{serverID}");
            }
        }

        public void ShutdownAllServers()
        {
            foreach (KeyValuePair<string, NamedPipeServerObject> kv in _servers)
            {
                kv.Value.IsStopping = true;
                try
                {
                    if (kv.Value.connection.IsConnected)
                        kv.Value.connection.Disconnect();
                }
                catch (Exception ex)
                {
                    OnError(kv.Value.ID, ex.Message, ex.StackTrace);
                    //Logger.Log.Alert($"[Internal] Failed to disconnect client#{kv.Value.ID}\n", Main.MESSAGEBOX_CAPTION);
                }
                finally
                {
                    kv.Value.connection.Close();
                    kv.Value.connection.Dispose();
                }
            }
            _servers.Clear();
            OnInternalMessage($"Shutdown all servers");
        }

        private void OnPipe_ClientConnected(IAsyncResult result)
        {
            NamedPipeServerObject serverObj = result.AsyncState as NamedPipeServerObject;
            if (serverObj.IsStopping == false)
            {
                lock (threadLock)
                {
                    if (serverObj.IsStopping == false)
                    {
                        serverObj.connection.EndWaitForConnection(result);

                        OnConnected(serverObj.ID);

                        serverObj.connection.BeginRead(serverObj.recvBuf, 0, READ_BUFFER_SIZE, OnPipe_Recv, serverObj);
                    }
                }
            }
            NamedPipeServerObject newPipe = new NamedPipeServerObject(PIPE_NAME, Guid.NewGuid().ToString());
            _servers[newPipe.ID] = newPipe;
            newPipe.connection.BeginWaitForConnection(OnPipe_ClientConnected, newPipe);
        }

        private void OnPipe_Recv(IAsyncResult result)
        {
            NamedPipeServerObject serverObj = result.AsyncState as NamedPipeServerObject;
            int numBytesRead = serverObj.connection.EndRead(result);
            OnInternalMessage($"Received {numBytesRead} bytes from Client#{serverObj.ID}");
            if (numBytesRead > 0)
            {
                // TO-DO:
                // append serverObj.MessageString with serverObj.recvBuf (encode to string)
                serverObj.MessageString.Append(Encoding.ASCII.GetString(serverObj.recvBuf, 0, numBytesRead));
                // check if serverObj.connection.IsMessageComplete
                if (serverObj.connection.IsMessageComplete)
                {
                    // if it is: push serverObj.MessageString to rtxtLog, then clear serverObj.MessageString (set to empty string, not null)
                    OnMessageReceived(serverObj.ID, serverObj.MessageString.ToString());
                    serverObj.MessageString.Clear();
                }
                else
                {
                    OnInternalMessage($"Received partial message '{serverObj.MessageString}' from Client#{serverObj.ID}");
                }
                // finally, regardless of conditional: queue up next read operation
                serverObj.connection.BeginRead(serverObj.recvBuf, 0, READ_BUFFER_SIZE, OnPipe_Recv, serverObj);
            }
            else
            {
                // TO-DO:
                // client disconnected, so close and dispose of serverObj.connection and serverObj itself.
                OnInternalMessage($"Client#{serverObj.ID} has disconnected");
                OnDisconnected(serverObj.ID);
                CloseServer(serverObj.ID);
            }
        }

        private void OnMessageReceived(string uuid, string message)
        {
            _synchronizationContext.Post(
               e => MessageReceivedEvent.SafeInvoke(this, (MessageReceivedEventArgs)e), new MessageReceivedEventArgs { UUID = uuid, Message = message });
        }

        private void OnConnected(string uuid)
        {
            _synchronizationContext.Post(
               e => ClientConnectedEvent.SafeInvoke(this, (ClientConnectedEventArgs)e), new ClientConnectedEventArgs { UUID = uuid });
        }

        private void OnDisconnected(string uuid)
        {
            _synchronizationContext.Post(
                e => ClientDisconnectedEvent.SafeInvoke(this, (ClientDisconnectedEventArgs)e), new ClientDisconnectedEventArgs { UUID = uuid });
        }

        private void OnError(string uuid, string message, string stackTrace)
        {
            _synchronizationContext.Post(
               e => ErrorEvent.SafeInvoke(this, (ErrorEventArgs)e), new ErrorEventArgs { UUID = uuid, Message = message, StackTrace = stackTrace });
        }

        private void OnInternalMessage(string message)
        {
            _synchronizationContext.Post(
               e => InternalEvent.SafeInvoke(this, (InternalMessageEventArgs)e), new InternalMessageEventArgs { Message = message });
        }
    }
}
