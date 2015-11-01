using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameFrame.TicTacToe
{
    public partial class GameBoard : Form
    {
        private TicTacToeGameRules rules;
        private Player<TicTacToeState> player1;
        private Player<TicTacToeState> player2;
        private Player<TicTacToeState> currentPlayer;
        private TicTacToeState currentState;

        public GameBoard()
        {
            InitializeComponent();
        }

        private void newGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rules = new TicTacToeGameRules();
            int? randomSeed = null;
            currentPlayer = player1 = new NegamaxPlayer<TicTacToeState>(rules, 1, 950, 2f, randomSeed);
            player2 = new NegamaxPlayer<TicTacToeState>(rules, -1, 950, 2f, randomSeed);
            UpdateState(TicTacToeState.Empty);
        }

        private void UpdateState(TicTacToeState newState)
        {
            currentState = newState;
            currentPlayer = currentPlayer == player1 ? player2 : player1;
            currentPlayer.MakeMoveAsync(currentState).ContinueWith(task => CheckWinner(task.Result));
        }

        private void CheckWinner(TicTacToeState state)
        {
            var outcome = rules.DetermineWinner(currentState);
            if (outcome == null) // the game is still going
            {
                UpdateState(state);
            }
            else if (outcome == 0f) MessageBox.Show("Tie game!");
            else if (outcome == -1f) MessageBox.Show(currentPlayer.Name + " wins!");
            //else if (outcome == 1f) MessageBox.Show("Tie Game!"); // implement if you can lose on your turn
        }
    }
}
