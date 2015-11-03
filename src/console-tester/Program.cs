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
            PlayNineMensMorris();
            //TestNineMensMorris();
        }

        static void PlayTicTacToe()
        {
            var rules = new TicTacToeGameRules();
            int? randomSeed = null;
            var p1 = new NegamaxPlayer<TicTacToeState>(rules, 1, 950, 2f, randomSeed);
            var p2 = new NegamaxPlayer<TicTacToeState>(rules, -1, 950, 2f, randomSeed);
            var state = TicTacToeState.Empty;
            PlayGame(rules, p1, p2, state);
        }

        static void PlayNineMensMorris()
        {
            var rules = new NineMensMorrisGameRules();
            int? randomSeed = 1;
            int millisecondsPerTurn = 9950;
            var p1 = new NegamaxPlayer<NineMensMorrisState>(rules, 1, millisecondsPerTurn, 1.5f, randomSeed);
            var p2 = new NegamaxPlayer<NineMensMorrisState>(rules, -1, millisecondsPerTurn, 1.5f, randomSeed);
            var state = NineMensMorrisState.Empty;
            PlayGame(rules, p1, p2, state);
        }

        static void TestNineMensMorris()
        {
            var rules = new NineMensMorrisGameRules();
            var p1 = new NegamaxPlayer<NineMensMorrisState>(rules, 1, 9950, 1.5f, randomSeed: 1);
            var state = new NineMensMorrisState()
            {
                ActivePlayer = -1,
                BlackUnplayed = 1,
                BlackRemaining = 9,
                WhiteUnplayed = 0,
                WhiteRemaining = 8,
                Board = new int?[] { 1, 1, -1, null, 1, null, null, -1, null, 1, -1, null, 1, null, -1, null, -1, null, -1, 1, 1, -1, 1, -1 },
                LastMove = Tuple.Create(-1, 12, -1),
                RepeatedState = false,
                StatesVisited = new HashSet<long>(),
            };
            var result = p1.MakeMove(state);
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
