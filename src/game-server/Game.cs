﻿using System;
using System.Collections.Generic;
using GameFrame;

namespace GameServer
{
    internal delegate void OnCompletedHandler<TState>(Game<TState> game, Utility results) where TState : IState;

    abstract class Game<TState> where TState : IState
    {
        protected readonly GameRules<TState> gameRules;

        private readonly ClientConnection player1;
        private readonly ClientConnection player2;
        private List<string> moves;
        private bool player1Ready = false;
        private bool player2Ready = false;
        private Action readyAction;
        private string currentStatus;
        private readonly int millisPerMove;

        protected Game(ClientConnection player1, ClientConnection player2, GameRules<TState> rules, int millisPerMove)
        {
            this.player1 = player1;
            this.player2 = player2;
            this.gameRules = rules;
            this.millisPerMove = millisPerMove;
        }

        public string GetDescription()
        {
            return gameRules.Name + " => " + player1.Name + " vs " + player2.Name;
        }

        public void Start()
        {
            PrepareNewGame();
        }

        private void PrepareNewGame()
        {
            moves = new List<string>();
            WhenAllReady(UpdateGameState);
            player1.Send($"prepare-new-game game={gameRules.Name} role={gameRules.Roles[0]} milliseconds-per-move={millisPerMove}");
            player2.Send($"prepare-new-game game={gameRules.Name} role={gameRules.Roles[1]} milliseconds-per-move={millisPerMove}");
        }

        private void UpdateGameState()
        {
            var gameState = GenerateWireState();
            if (GetUtility(gameState).IsTerminal)
            {
                WhenAllReady(AnnounceResults);
            }
            else
            {
                WhenAllReady(GameLoop);
            }
            player1.Send("update-game-state state=" + gameState);
            player2.Send("update-game-state state=" + gameState);
        }

        private string GenerateWireState()
        {
            return string.Join(";", moves);
        }

        private Utility GetUtility(string gameState)
        {
            var state = BuildState(gameState);
            var utility = gameRules.CalculateUtility(state);
            return utility;
        }

        protected abstract TState BuildState(string serverState);

        private void GameLoop()
        {
            var playerToMove = moves.Count % 2 == 0 ? player1 : player2;
            playerToMove.OnReceive += MakeMove;
            playerToMove.Send("make-move");
        }

        private void MakeMove(ClientConnection client, string value)
        {
            var args = value.Split(' ');
            if (args.Length != 2 || args[0] != "move")
            {
                throw new Exception("Invalid message: expected move.");
            }
            client.OnReceive -= MakeMove;
            moves.Add(args[1]);
            UpdateGameState();
        }

        private void AnnounceResults()
        {
            var gameState = GenerateWireState();
            var utility = GetUtility(gameState);
            player1.Send($"announce-results {utility}");
            player2.Send($"announce-results {utility}");
            if (OnCompleted != null)
            {
                OnCompleted(this, utility);
            }
        }

        private void WhenAllReady(Action nextAction)
        {
            currentStatus = "waiting";
            readyAction = nextAction;
            player1Ready = false;
            player2Ready = false;
            player1.OnReceive += ReadyListener;
            player2.OnReceive += ReadyListener;
        }

        private void ReadyListener(ClientConnection client, string value)
        {
            if (client == player1 && value == "ready")
            {
                player1Ready = true;
                player1.OnReceive -= ReadyListener;
            }
            else if (client == player2 && value == "ready")
            {
                player2Ready = true;
                player2.OnReceive -= ReadyListener;
            }
            else
            {
                throw new Exception("Invalid message: expected 'ready'.");
            }
            lock (currentStatus)
            {
                if (player1Ready && player2Ready && currentStatus == "waiting")
                {
                    currentStatus = "acting";
                    readyAction();
                }
            }
        }

        public event OnCompletedHandler<TState> OnCompleted;
    }
}
