using System;
using System.Net;
using System.Threading;

namespace GameServer
{
    // https://msdn.microsoft.com/en-us/library/fx6588te(v=vs.110).aspx
    static class GameServerProgram
    {
        static readonly ManualResetEvent keepRunning = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            try
            {
                var server = new Server(Console.Out);
                server.Start(Dns.GetHostName(), 11873);
                keepRunning.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            
            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }
    }
}
