// credit: Ifat Chitin Morrison [https://www.codeproject.com/articles/864679/creating-a-server-using-named-pipes]
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.ComponentModel;

namespace NinMods.Logging
{
    public class ClientConnectedEventArgs : EventArgs
    {
    }

    public class ClientDisconnectedEventArgs : EventArgs
    {
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    public class PipeClient
    {
        const int READ_BUFFER_SIZE = 8192;
        const int SEND_BUFFER_SIZE = 8192;
        const int CONNECT_TIMEOUT_MILLISECONDS = 60 * 1000;

        private bool isValid = true;
        private NamedPipeClientStream connection;
        private byte[] recvBuf;

        private StringBuilder MessageString;

        // for firing events in thread-safe way
        private readonly SynchronizationContext _synchronizationContext;

        public event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;
        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        // WARNING:
        // this *must* be instantiated on the main thread!
        public PipeClient(string pipeName)
        {
            connection = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            recvBuf = new byte[READ_BUFFER_SIZE];
            MessageString = new StringBuilder("");

            // saves the context of this thread so that when we later post to it from any thread, we will be posting to this thread
            // (used for firing events)
            _synchronizationContext = AsyncOperationManager.SynchronizationContext;
        }

        public bool Start()
        {
            try
            {
                connection.Connect(CONNECT_TIMEOUT_MILLISECONDS);
                OnConnected();
                connection.BeginRead(recvBuf, 0, READ_BUFFER_SIZE, OnPipe_Recv, this);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log.WriteError("Could not connect to pipe server");
                return false;
            }
        }

        public bool Shutdown()
        {
            connection.WaitForPipeDrain();
            connection.Close();
            connection = null;
            // TO-DO:
            // actually do something with this. should probably check it in the handlers below and early exit if set.
            isValid = false;
            return true;
        }

        public void SendMessage(string msg)
        {
            byte[] msgBytes = Encoding.ASCII.GetBytes(msg);
            connection.BeginWrite(msgBytes, 0, msgBytes.Length, OnPipe_Sent, this);
        }

        private void OnPipe_Recv(IAsyncResult result)
        {
            PipeClient clientObj = result.AsyncState as PipeClient;
            int numBytesRead = clientObj.connection.EndRead(result);

            if (numBytesRead > 0)
            {
                // append clientObj.MessageString with clientObj.recvBuf (encode to string)
                clientObj.MessageString.Append(Encoding.ASCII.GetString(clientObj.recvBuf, 0, numBytesRead));
                // check if clientObj.connection.IsMessageComplete
                if (clientObj.connection.IsMessageComplete)
                {
                    // if it is: push clientObj.MessageString to rtxtLog, then clear clientObj.MessageString (set to empty string, not null)
                    OnMessageReceived(clientObj.MessageString.ToString() + "\n");
                    clientObj.MessageString.Clear();
                }
                else
                {
                    Logger.Log.WriteThreaded($"Received partial message from pipe server '{clientObj.MessageString}'");
                }
                // finally, regardless of conditional: queue up next read operation
                clientObj.connection.BeginRead(clientObj.recvBuf, 0, READ_BUFFER_SIZE, OnPipe_Recv, clientObj);
            }
            else
            {
                // server disconnected, so close and dispose of clientObj.connection and clientObj itself.
                Logger.Log.WriteThreaded($"Pipe server disconnected unexpectedly");
                isValid = false;
                OnDisconnected();
                clientObj.connection.Close();
                clientObj = null;
            }
        }

        private void OnPipe_Sent(IAsyncResult result)
        {
            PipeClient clientObj = result.AsyncState as PipeClient;
            clientObj.connection.EndWrite(result);

            //Logger.Log.WriteThreaded($"[Internal] Finished sending message to server\n");
        }

        private void OnMessageReceived(string message)
        {
            _synchronizationContext.Post(
               e => MessageReceivedEvent.SafeInvoke(this, (MessageReceivedEventArgs)e), new MessageReceivedEventArgs { Message = message});
        }

        private void OnConnected()
        {
            _synchronizationContext.Post(
               e => ClientConnectedEvent.SafeInvoke(this, (ClientConnectedEventArgs)e), new ClientConnectedEventArgs());
        }

        private void OnDisconnected()
        {
            _synchronizationContext.Post(
                e => ClientDisconnectedEvent.SafeInvoke(this, (ClientDisconnectedEventArgs)e), new ClientDisconnectedEventArgs());
        }
    }
}