using System;
using System.Threading;

namespace GameServer
{
    class GameServerBotProgram
    {
        private static readonly ManualResetEvent responseWaiter = new ManualResetEvent(false);
        
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Please enter a bot name as the only argument.");
                return;
            }
            try
            {
                new GameServerBot(args[0]).Connect();
                responseWaiter.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
