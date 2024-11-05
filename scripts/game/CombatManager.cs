using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class CombatManager : Node2D
{
    private readonly List<Player> _turnOrder = new();

    private Player _currentPlayer;
    private Player _firstAlly;
    private Player _firstEnemy;
    private Player _lastAllyPlayer;
    private Player _lastEnemyPlayer;

    private Button _nextButton;

    public override void _Ready()
    {
        _nextButton = GetNode<Button>("../Button");
        foreach (
            Player player in GameUtils.CombatEntities.OrderByDescending(static player =>
                player.Initiative
            )
        )
        {
            _currentPlayer ??= player;
            _turnOrder.Add(player);
            AddChild(player);
        }

        _currentPlayer.TurnIndicator.Visible = true;

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

        if (_currentPlayer.IsAlly)
        {
            _currentPlayer.SlideHandIn();
            _currentPlayer.EnablePlayerHand();
            _lastAllyPlayer = _currentPlayer;
        }
        else
        {
            GetTree().CreateTimer(1).Timeout += _currentPlayer.EnemyPlayHand;
            _nextButton.Disabled = true;
            _firstAlly.SlideHandDisabled();
            _lastEnemyPlayer = _currentPlayer;
            _currentPlayer.EnemyFinishedTurn += OnEnemyFinishedTurn;
        }

        _firstAlly.SlideBoardIn();
        _firstEnemy.SlideBoardIn();
    }

    public void NextTurn()
    {
        // Disconnect the event handler from current player if it's an enemy
        if (!_currentPlayer.IsAlly)
        {
            _currentPlayer.EnemyFinishedTurn -= OnEnemyFinishedTurn;
        }

        Player nextPlayer = _turnOrder[1];

        // Track last enemy if current player is an enemy
        if (_currentPlayer.IsAlly)
        {
            _lastAllyPlayer = _currentPlayer;
        }
        else
        {
            _lastEnemyPlayer = _currentPlayer;
        }

        // Handle current player's exit
        if (!nextPlayer.IsAlly && _currentPlayer.IsAlly)
        {
            _currentPlayer.SlideHandDisabled();
        }
        else
        {
            _currentPlayer.SlideHandOut();
        }

        // Handle special ally-related transitions
        if (!_currentPlayer.IsAlly && nextPlayer.IsAlly)
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

        // Update and handle enemy board transitions
        Logger.Debug($"_lastAllyPlayer: {_lastAllyPlayer?.PlayerName}");
        Logger.Debug($"_lastEnemyPlayer: {_lastEnemyPlayer?.PlayerName}");
        if (!nextPlayer.IsAlly)
        {
            // If there was a previous enemy, hide their board
            _lastEnemyPlayer?.SlideBoardOut();
        }
        else
        {
            _lastAllyPlayer?.SlideBoardOut();
        }

        _currentPlayer.EndTurn();
        _turnOrder.Remove(_currentPlayer);
        _turnOrder.Add(_currentPlayer);
        _currentPlayer = nextPlayer;

        // Handle new player's entrance
        if (_currentPlayer.IsAlly)
        {
            _nextButton.Disabled = false;
            _currentPlayer.SlideHandIn();
        }
        else
        {
            _nextButton.Disabled = true;
            _currentPlayer.EnemyFinishedTurn += OnEnemyFinishedTurn;
        }

        _currentPlayer.SlideBoardIn();

        _currentPlayer.StartTurn();
    }

    private void OnEnemyFinishedTurn()
    {
        GetTree().CreateTimer(1.5).Timeout += NextTurn;
    }
}
