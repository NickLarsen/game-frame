using System;
using System.Linq;
using GameFrame;
using GameFrame.Games;

namespace ConsoleTester
{
    class Program
    {
        private static readonly int? debugRandomSeed = 123;

        static void Main(string[] args)
        {
            PlayTicTacToe();
            //PlayConnectFour();
            //TestConnectFour();
            //PlayNineMensMorris();
            //TestNineMensMorris();
        }

        static void PlayTicTacToe()
        {
            var rules = new TicTacToeGameRules();
            var p1 = new NegamaxPlayer<TicTacToeState>(rules, rules.Roles[0], 950, 2f, debugRandomSeed);
            var p2 = new NegamaxPlayer<TicTacToeState>(rules, rules.Roles[1], 950, 2f, debugRandomSeed);
            var state = TicTacToeState.Empty;
            PlayGame(rules, p1, p2, state);
        }

        static void PlayConnectFour()
        {
            var rules = new ConnectFourGameRules();
            var p1 = new NegamaxPlayer<ConnectFourState>(rules, rules.Roles[0], 10000, 2f, debugRandomSeed);
            var p2 = new NegamaxPlayer<ConnectFourState>(rules, rules.Roles[1], 10000, 2f, debugRandomSeed);
            var state = ConnectFourState.Empty;
            PlayGame(rules, p1, p2, state);
        }

        static void TestConnectFour()
        {
            var rules = new ConnectFourGameRules();
            var p1 = new NegamaxPlayer<ConnectFourState>(rules, rules.Roles[1], 10000, 2f, debugRandomSeed);
            var stateString = "6;7;0;1;18";
            var state = BuildConnectFourState(stateString);
            var successors = rules.Expand(state);//.Where(m => m.Board[12] == 1 && m.Board[13] == 0);
            int h = 1;
            foreach (var s in successors)//.Skip(0).Take(3))
            {
                Console.WriteLine(h++);
                Console.WriteLine(s.ToString());
            }
            var result = p1.MakeMove(state);
            int i = 0;
        }

        private static ConnectFourState BuildConnectFourState(string serverState)
        {
            var moves = serverState.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(m => int.Parse(m))
                .ToArray();
            var state = ConnectFourState.Empty;
            foreach (var move in moves)
            {
                state = state.ApplyMove(move);
                state.PreRun(); // updates visited states for draw tracking
            }
            return state;
        }

        static void PlayNineMensMorris()
        {
            var rules = new NineMensMorrisGameRules();
            int millisecondsPerTurn = 10000;
            var p1 = new NegamaxPlayer<NineMensMorrisState>(rules, rules.Roles[0], millisecondsPerTurn, 1.5f, debugRandomSeed);
            var p2 = new NegamaxPlayer<NineMensMorrisState>(rules, rules.Roles[1], millisecondsPerTurn, 1.5f, debugRandomSeed);
            var state = NineMensMorrisState.Empty;
            PlayGame(rules, p1, p2, state);
        }

        static void TestNineMensMorris()
        {
            var rules = new NineMensMorrisGameRules();
            var p1 = new NegamaxPlayer<NineMensMorrisState>(rules, rules.Roles[0], 9950, 1.5f, debugRandomSeed);
            var stateString = "";
            var state = BuildNineMensMorrisState(stateString);
            //var successors = rules.Expand(state);//.Where(m => m.Board[12] == 1 && m.Board[13] == 0);
            //int h = 1;
            //foreach (var s in successors)//.Skip(0).Take(3))
            //{
            //    Console.WriteLine(h++);
            //    Console.WriteLine(s.ToString());
            //}
            var result = p1.MakeMove(state);
            int i = 0;
        }

        private static NineMensMorrisState BuildNineMensMorrisState(string serverState)
        {
            var moves = serverState.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Split(','))
                .Select(m => Tuple.Create(int.Parse(m[0]), int.Parse(m[1]), int.Parse(m[2])))
                .ToArray();
            var state = NineMensMorrisState.Empty;
            foreach (var move in moves)
            {
                state = state.ApplyMove(move);
                state.PreRun(); // updates visited states for draw tracking
            }
            return state;
        }

        static void PlayGame<T>(GameRules<T> rules, Player<T> p1, Player<T> p2, T state) where T : IState
        {
            Console.WriteLine(state.ToString());
            var currentPlayer = state.ActivePlayer == 1 ? p1 : p2;
            var utility = new Utility(rules.Roles);
            while (!utility.IsTerminal)
            {
                state = currentPlayer.MakeMove(state);
                Console.WriteLine($"Move: {currentPlayer.Role} = " + state.LastMoveDescription());
                Console.WriteLine(state.ToString());
                currentPlayer = currentPlayer == p1 ? p2 : p1;
                utility = rules.CalculateUtility(state);
            }
            Console.WriteLine($"Result:  {utility}");
        }
    }
}
