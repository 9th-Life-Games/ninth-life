using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class CombatManager : Node2D
{
    private readonly CombatUiManager _combatUiManager = new();
    private readonly List<Player> _turnOrder = new();

    private Player _currentPlayer;
    private Player _firstAlly;
    private Player _firstEnemy;

    public override void _Ready()
    {
        _combatUiManager.SetNextButton(GetNode<Button>("../Button"));
        InitializePlayers();
        SetInitialAlliesAndEnemies();
    }

    private void InitializePlayers()
    {
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

        if (!_currentPlayer.IsAlly)
        {
            InitEnemyAi();
        }
    }

    private void SetInitialAlliesAndEnemies()
    {
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

        _combatUiManager.InitPlayerUi(_currentPlayer, _firstAlly, _firstEnemy);
    }

    private void InitEnemyAi()
    {
        GetTree().CreateTimer(1).Timeout += _currentPlayer.EnemyPlayHand;
        _currentPlayer.EnemyFinishedTurn += OnEnemyFinishedTurn;
    }

    public void NextTurn()
    {
        // Disconnect the event handler from current player if it's an enemy
        if (!_currentPlayer.IsAlly)
        {
            _currentPlayer.EnemyFinishedTurn -= OnEnemyFinishedTurn;
        }

        Player nextPlayer = _turnOrder[1];

        // Handle current player's exit
        _combatUiManager.HandleCurrentAllyHandExit(_currentPlayer, nextPlayer);


        // Hide lastAlly hand to swap with nextAlly hand as it gets shown
        if (!_currentPlayer.IsAlly && nextPlayer.IsAlly)
        {
            Player lastAlly = _turnOrder.FindLast(player => player.IsAlly);
            _combatUiManager.HideLastAllyHand(lastAlly);
        }

        // Update and handle enemy board transitions
        _combatUiManager.HideLastPlayerBoard(nextPlayer.IsAlly);

        _currentPlayer.EndTurn();
        _turnOrder.Remove(_currentPlayer);
        _turnOrder.Add(_currentPlayer);
        _currentPlayer = nextPlayer;

        // Handle new player's entrance
        if (!_currentPlayer.IsAlly)
        {
            _currentPlayer.EnemyFinishedTurn += OnEnemyFinishedTurn;
        }

        _combatUiManager.ShowPlayerUi(_currentPlayer);

        _currentPlayer.StartTurn();
    }

    private void OnEnemyFinishedTurn()
    {
        GetTree().CreateTimer(1.5).Timeout += NextTurn;
    }
}
