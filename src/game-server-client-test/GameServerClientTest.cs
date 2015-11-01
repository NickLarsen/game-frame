using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace game_server_client_test
{
    // https://msdn.microsoft.com/en-us/library/bew39x2a(v=vs.110).aspx
    static class GameServerClientTest
    {
        static void Main(string[] args)
        {
            try
            {
                RunClient();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void RunClient()
        {
            var connection = new SocketConnection();
            connection.OnReceive += HandleReceive;
            connection.Connect();
            while (true)
            {
                var input = Console.ReadLine();
                if (input == "exit") break;
                connection.Send(input);
            }
            connection.Close();
        }

        private static void HandleReceive(string value)
        {
            Console.WriteLine(value);
        }
    }

    public delegate void ReceivedEventHandler(string value);

    class SocketConnection : IDisposable
    {
        private Socket socket;
        private const int BufferSize = 1024;
        private byte[] buffer = new byte[BufferSize];
        private StringBuilder sb = new StringBuilder();

        public void Connect()
        {
            Write("Connecting socket...");
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(remoteEP);
            InitiateReceive();
        }

        public void Send(string value)
        {
            if (socket.IsConnected())
            {
                byte[] byteData = Encoding.ASCII.GetBytes(value);
                socket.Send(byteData);
            }
            else
            {
                Write("Unable to send: connection closed.");
            }
        }

        public void Close()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        private void InitiateReceive()
        {
            if (socket.IsConnected())
            {
                socket.BeginReceive(buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, null);
                Write("Waiting for data...");
            }
            else
            {
                Connect();
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            // TODO: catch forcibly closed connections and gracefully disconnect
            int bytesRead = socket.EndReceive(ar);
            Write("ReceivedCallback: " + bytesRead);
            if (bytesRead > 0)
            {
                sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            }
            else
            {
                var message = sb.ToString();
                sb.Clear();
                if (OnReceive != null)
                {
                    OnReceive(message);
                }
            }
            InitiateReceive();
        }

        public event ReceivedEventHandler OnReceive;

        public void Dispose()
        {
            socket.Dispose();
        }

        private void Write(string value)
        {
            Console.WriteLine("[{0}] " + value, DateTime.UtcNow);
        }
    }

    static class SocketExtensions
    {
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }
    }
}
