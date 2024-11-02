using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game
{
    public partial class CombatManager : Node2D
    {
        private readonly List<Player> _turnOrder = new();

        private Player _firstAlly;
        private Player _firstEnemy;
        private Player _lastAllyPlayer;
        private Player _lastEnemyPlayer;

        public Player CurrentPlayer { get; set; }

        public override void _Ready()
        {
            foreach (
                Player player in GameUtils.CombatEntities.OrderByDescending(static player =>
                    player.Initiative
                )
            )
            {
                CurrentPlayer ??= player;
                _turnOrder.Add(player);
                AddChild(player);
            }

            CurrentPlayer.TurnIndicator.Visible = true;

            foreach (Player player in GetChildren().OfType<Player>())
            {
                if (player.IsAlly)
                {
                    _firstAlly ??= player;
                }
                else
                {
                    _firstEnemy ??= player;
                }
            }
            if (CurrentPlayer.IsAlly)
            {
                CurrentPlayer.SlideHandIn();
                CurrentPlayer.EnablePlayerHand();
                _lastAllyPlayer = CurrentPlayer;
            }
            else
            {
                _firstAlly.SlideHandDisabled();
                _lastEnemyPlayer = CurrentPlayer;
            }

            _firstAlly.SlideBoardIn();
            _firstEnemy.SlideBoardIn();
        }

        public void NextTurn()
        {
            Player nextPlayer = _turnOrder[1];

            // Track last enemy if current player is an enemy
            if (CurrentPlayer.IsAlly)
            {
                _lastAllyPlayer = CurrentPlayer;
            }
            else
            {
                _lastEnemyPlayer = CurrentPlayer;
            }

            // Handle current player's exit
            if (!nextPlayer.IsAlly && CurrentPlayer.IsAlly)
            {
                CurrentPlayer.SlideHandDisabled();
            }
            else
            {
                CurrentPlayer.SlideHandOut();
                // CurrentPlayer.SlideBoardOut(); // Always slide out current player's board
            }

            // Handle special ally-related transitions
            if (!CurrentPlayer.IsAlly && nextPlayer.IsAlly)
            {
                for (int i = _turnOrder.Count - 1; i >= 0; i--)
                {
                    if (!_turnOrder[i].IsAlly)
                    {
                        continue;
                    }
                    _turnOrder[i].SlideHandOut();
                    // _turnOrder[i].SlideBoardOut();
                    break;
                }
            }

            // Update and handle enemy board transitions
            Logger.Debug($"_lastAllyPlayer: {_lastAllyPlayer?.PlayerName}");
            Logger.Debug($"_lastEnemyPlayer: {_lastEnemyPlayer?.PlayerName}");
            if (!nextPlayer.IsAlly)
            {
                // If there was a previous enemy, hide their board
                Logger.Debug(
                    "Next player is an enemy, so slide the last enemies board out so we can slide the next enemies board in"
                );
                _lastEnemyPlayer?.SlideBoardOut();
            }
            else
            {
                Logger.Debug(
                    "Next player is an ally, so slide the last allies board out so we can slide the next allies board in"
                );
                _lastAllyPlayer?.SlideBoardOut();
            }

            CurrentPlayer.EndTurn();
            _ = _turnOrder.Remove(CurrentPlayer);
            _turnOrder.Add(CurrentPlayer);
            CurrentPlayer = nextPlayer;

            // Handle new player's entrance
            if (CurrentPlayer.IsAlly)
            {
                CurrentPlayer.SlideHandIn();
            }

            CurrentPlayer.SlideBoardIn();

            CurrentPlayer.StartTurn();
        }
    }
}
