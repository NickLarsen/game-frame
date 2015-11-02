using System;
using System.Net;
using System.Threading;
using GameServer;

namespace GameServerManagementConsole
{
    static class GameServerManagementConsoleProgram
    {
        static readonly ManualResetEvent responseWaiter = new ManualResetEvent(false);

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
            Console.Write("Please input a bot name: ");
            var name = Console.ReadLine();
            var connection = new GameServerConnection(name);
            connection.OnReceive += HandleReceive;
            connection.Connect(Dns.GetHostName(), 11873);
            while (true)
            {
                responseWaiter.Reset();
                Console.Write(connection.CommandLineDescription + "> ");
                var input = Console.ReadLine();
                if (input == "exit") break;
                connection.Send(input);
                responseWaiter.WaitOne();
            }
            connection.Close();
        }

        private static void HandleReceive(string value)
        {
            Console.WriteLine(value);
            responseWaiter.Set();
        }
    }
}
