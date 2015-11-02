using System;
using System.Net.Sockets;
using System.Text;

namespace GameServer
{
    public delegate void ReceivedEventHandler(ClientConnection client, string value);
    public delegate void ConnectedHandler(ClientConnection client);
    public delegate void DisconnectedHandler(ClientConnection client, Exception exception);

    public class ClientConnection : IDisposable
    {
        private char responseTerminator = '\0';
        private readonly Socket socket;
        private const int BufferSize = 1024;
        private readonly byte[] buffer = new byte[BufferSize];
        private readonly StringBuilder sb = new StringBuilder();
        public string Name { get; private set; }

        public ClientConnection(Socket remoteSocket)
        {
            socket = remoteSocket;
        }

        public void InitiateProtocolHandshake()
        {
            InitiateReceive(HandshakeCallback);
        }

        private void InitiateReceive(AsyncCallback callback)
        {
            HandleDisconnect(() =>
            {
                socket.BeginReceive(buffer, 0, BufferSize, SocketFlags.None, callback, null);
            });
        }

        private void HandshakeCallback(IAsyncResult ar)
        {
            HandleDisconnect(() =>
            {
                int bytesRead = socket.EndReceive(ar);
                Name = Encoding.ASCII.GetString(buffer, 0, bytesRead - 1);
                // TODO: handle error cases
                InitiateReceive(ReceiveCallback);
                Send(Name);
                if (OnConnected != null)
                {
                    OnConnected(this);
                }
            });
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            HandleDisconnect(() =>
            {
                // TODO: catch forcibly closed connections and gracefully disconnect
                int bytesRead = socket.EndReceive(ar);
                if (bytesRead > 0)
                {
                    var value = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    sb.Append(value);
                    if (value[bytesRead - 1] == responseTerminator)
                    {
                        var message = sb.Remove(sb.Length - 1, 1).ToString();
                        sb.Clear();
                        if (OnReceive != null)
                        {
                            OnReceive(this, message);
                        }
                    }
                }
                InitiateReceive(ReceiveCallback);
            });
        }

        public event ReceivedEventHandler OnReceive;
        public event ConnectedHandler OnConnected;
        public event DisconnectedHandler OnDisconnected;

        public void Dispose()
        {
            socket.Dispose();
        }

        public void Send(string value)
        {
            HandleDisconnect(() =>
            {
                byte[] byteData = Encoding.ASCII.GetBytes(value + responseTerminator);
                socket.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, null);
            });
        }

        private void SendCallback(IAsyncResult ar)
        {
            HandleDisconnect(() =>
            {
                socket.EndSend(ar);
            });
        }

        private void HandleDisconnect(Action action)
        {
            try
            {
                action();
            }
            catch (SocketException se)
            {
                // this socket failed, inform those who care
                if (OnDisconnected != null)
                {
                    OnDisconnected(this, se);
                }
            }
        }
    }
}
