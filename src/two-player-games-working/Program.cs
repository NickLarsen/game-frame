﻿using System;

namespace two_player_games_working
{
    class Program
    {
        private static bool moveAtATime = false;

        static void Main(string[] args)
        {
            moveAtATime = args.Any(m => m == "-m");
            PlayTicTacToe();
        }

        static void PlayTicTacToe()
        {
            var rules = new TicTacToeGameRules();
            var p1 = new NegamaxPlayer<TicTacToeState>(rules, 1, 950, 2f);
            var p2 = new NegamaxPlayer<TicTacToeState>(rules, -1, 950, 2f);
            var state = TicTacToeState.Empty;
            PlayGame(rules, p1, p2, state);
        }
        static void PlayGame<T>(GameRules<T> rules, Player<T> p1, Player<T> p2, T state) where T : IState
        {
            Console.WriteLine(state.ToString());
            var currentPlayer = state.ActivePlayer == 1 ? p1 : p2;
            while (rules.DetermineWinner(state) == null)
            {
                if (moveAtATime)
                {
                    Console.WriteLine("move at a time, press enter to continue");
                    Console.ReadLine();
                }
                state = currentPlayer.MakeMove(state);
                Console.WriteLine(-state.ActivePlayer + ": ");
                Console.WriteLine(state.ToString());
                currentPlayer = currentPlayer == p1 ? p2 : p1;
            }
            Console.WriteLine("Winner = " + rules.DetermineWinner(state));
        }
    }
}
