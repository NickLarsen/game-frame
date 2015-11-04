using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GameServer
{
    public delegate void ReceivedEventHandler(string value);

    public class GameServerConnection : IDisposable
    {
        private char responseTerminator = '\0';
        private Socket socket;
        private const int BufferSize = 1024;
        private readonly byte[] buffer = new byte[BufferSize];
        private readonly StringBuilder sb = new StringBuilder();
        public string CommandLineDescription { get; private set; }
        private readonly string name;

        public GameServerConnection(string name)
        {
            this.name = name;
        }

        public void Connect(string ipAddress, int port)
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(ipAddress);
            IPAddress ipAddressReal = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddressReal, port);
            CommandLineDescription = remoteEP.ToString();
            socket = new Socket(ipAddressReal.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(remoteEP);
            InitiateReceive(HandshakeCallback);
            Send(name);
        }

        public void Send(string value)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(value + responseTerminator);
            socket.Send(byteData);
        }

        public void Close()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        private void InitiateReceive(AsyncCallback callback)
        {
            socket.BeginReceive(buffer, 0, BufferSize, SocketFlags.None, callback, null);
        }

        private void HandshakeCallback(IAsyncResult ar)
        {
            int bytesRead = socket.EndReceive(ar);
            if (bytesRead > 0)
            {
                // TODO: convert to stream and end messages on FIRST null byte instead of assuming null byte at end
                var value = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                sb.Append(value);
                if (value[bytesRead - 1] == responseTerminator)
                {
                    var message = sb.Remove(sb.Length - 1, 1).ToString();
                    // TODO: handle error cases
                    if (message != name)
                    {
                        throw new Exception("Unable to complete handshake: " + message);
                    }
                    sb.Clear();
                    InitiateReceive(ReceiveCallback);
                }
                else
                {
                    InitiateReceive(HandshakeCallback);
                }
            }
            else
            {
                InitiateReceive(HandshakeCallback);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            // TODO: catch forcibly closed connections and gracefully disconnect
            int bytesRead = socket.EndReceive(ar);
            if (bytesRead > 0)
            {
                // TODO: convert to stream and end messages on FIRST null byte instead of assuming null byte at end
                var value = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                sb.Append(value);
                if (value[bytesRead - 1] == responseTerminator)
                {
                    var message = sb.Remove(sb.Length - 1, 1).ToString();
                    sb.Clear();
                    if (OnReceive != null)
                    {
                        OnReceive(message);
                    }
                }
            }
            InitiateReceive(ReceiveCallback);
        }

        public event ReceivedEventHandler OnReceive;

        public void Dispose()
        {
            socket.Dispose();
        }
    }
}
