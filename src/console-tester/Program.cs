using System;
using System.Collections.Generic;
using System.Linq;
using GameFrame;
using GameFrame.Games;

namespace ConsoleTester
{
    class Program
    {
        private static bool debug = false;

        static void Main(string[] args)
        {
            debug = args.Any(m => m == "-debug");
            //PlayTicTacToe();
            //PlayConnectFour();
            TestConnectFour();
            //PlayNineMensMorris();
            //TestNineMensMorris();
        }

        static void PlayTicTacToe()
        {
            var rules = new TicTacToeGameRules();
            var p1 = new NegamaxPlayer<TicTacToeState>(rules, 1, 950, 2f, randomSeed: null);
            var p2 = new NegamaxPlayer<TicTacToeState>(rules, -1, 950, 2f, randomSeed: null);
            var state = TicTacToeState.Empty;
            PlayGame(rules, p1, p2, state);
        }

        static void PlayConnectFour()
        {
            var rules = new ConnectFourGameRules();
            var p1 = new NegamaxPlayer<ConnectFourState>(rules, 1, 10000, 2f, randomSeed: 1);
            var p2 = new NegamaxPlayer<ConnectFourState>(rules, -1, 10000, 2f, randomSeed: 1);
            var state = ConnectFourState.Empty;
            PlayGame(rules, p1, p2, state);
        }

        static void TestConnectFour()
        {
            var rules = new ConnectFourGameRules();
            var p1 = new NegamaxPlayer<ConnectFourState>(rules, -1, 10000, 2f, randomSeed: 1);
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
            var p1 = new NegamaxPlayer<NineMensMorrisState>(rules, 1, millisecondsPerTurn, 1.5f, randomSeed: 2);
            var p2 = new NegamaxPlayer<NineMensMorrisState>(rules, -1, millisecondsPerTurn, 1.5f, randomSeed: 2);
            var state = NineMensMorrisState.Empty;
            PlayGame(rules, p1, p2, state);
        }

        static void TestNineMensMorris()
        {
            var rules = new NineMensMorrisGameRules();
            var p1 = new NegamaxPlayer<NineMensMorrisState>(rules, 1, 9950, 1.5f, randomSeed: 2);
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
            while (rules.DetermineWinner(state) == null)
            {
                state = currentPlayer.MakeMove(state);
                var lastMovePlayerName = state.ActivePlayer == 1 ? rules.SecondPlayerName : rules.FirstPlayerName;
                Console.WriteLine($"Move: {lastMovePlayerName} = " + state.LastMoveDescription());
                Console.WriteLine(state.ToString());
                currentPlayer = currentPlayer == p1 ? p2 : p1;
                if (debug)
                {
                    while (true)
                    {
                        Console.WriteLine("debugging, enter command, empty line to move on");
                        string input = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(input)) break;
                        switch (input)
                        {
                            case "state": 
                                state.WriteDebugInfo(Console.Out);
                                break;
                            default:
                                Console.WriteLine("Unrecognized command");
                                break;
                        }
                    }
                }
            }
            var winner = rules.DetermineWinner(state);
            if (winner == 0f) Console.WriteLine("Result: Tie");
            if (winner < 0f) Console.WriteLine($"Result: {rules.FirstPlayerName} wins!");
            if (winner > 0f) Console.WriteLine($"Result: {rules.SecondPlayerName} wins!");
        }
    }
}
