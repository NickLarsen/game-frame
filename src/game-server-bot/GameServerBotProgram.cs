using System;
using System.Threading;

namespace GameServer
{
    class GameServerBotProgram
    {
        private static readonly ManualResetEvent responseWaiter = new ManualResetEvent(false);
        
        static void Main(string[] args)
        {
            try
            {
                new GameServerBot().Connect();
                responseWaiter.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
