using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game
{
    public partial class CombatManager : Node2D
    {
        [Signal]
        public delegate void CombatEntitiesAddedEventHandler();

        private readonly List<Player> _turnOrder = new();

        private Player _firstAlly;

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

            _ = EmitSignal(SignalName.CombatEntitiesAdded);

            CurrentPlayer.TurnIndicator.Visible = true;

            if (CurrentPlayer.IsAlly)
            {
                CurrentPlayer.SlideHandIn();
            }
            else
            {
                foreach (Player player in GetChildren().OfType<Player>())
                {
                    if (player.IsAlly)
                    {
                        _firstAlly ??= player;
                    }
                }

                _firstAlly.SlideHandDisabled();
            }
        }

        public void NextTurn()
        {
            Player nextPlayer = _turnOrder[1];
            if (!nextPlayer.IsAlly)
            {
                CurrentPlayer.SlideHandDisabled();
            }
            else
            {
                CurrentPlayer.SlideHandOut();
            }

            if (!CurrentPlayer.IsAlly && nextPlayer.IsAlly)
            {
                for (int i = _turnOrder.Count - 1; i >= 0; i--)
                {
                    if (!_turnOrder[i].IsAlly)
                    {
                        continue;
                    }

                    _turnOrder[i].SlideHandOut();
                    break;
                }
            }

            CurrentPlayer.EndTurn();
            _ = _turnOrder.Remove(CurrentPlayer);
            _turnOrder.Add(CurrentPlayer);
            CurrentPlayer = nextPlayer;
            if (CurrentPlayer.IsAlly)
            {
                CurrentPlayer.SlideHandIn();
            }

            CurrentPlayer.StartTurn();
        }
    }
}
