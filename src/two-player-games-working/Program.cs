using System;

namespace two_player_games_working
{
    class Program
    {
        static void Main(string[] args)
        {
            var rules = new TicTacToeGameRules();
            var p1 = new NegamaxPlayer<TicTacToeState>(rules, 1);
            var p2 = new NegamaxPlayer<TicTacToeState>(rules, -1);
            var state = TicTacToeState.Empty;
            Console.WriteLine(state.ToString());
            var currentPlayer = state.ActivePlayer == 1 ? p1 : p2;
            while (rules.DetermineWinner(state) == null)
            {
                state = currentPlayer.MakeMove(state);
                Console.WriteLine(-state.ActivePlayer + ": ");
                Console.WriteLine(state.ToString());
                currentPlayer = currentPlayer == p1 ? p2 : p1;
            }
            Console.WriteLine("Winner = " + rules.DetermineWinner(state));
        }
    }
}
