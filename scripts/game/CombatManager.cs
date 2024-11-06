using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class CombatManager : Node2D
{
    private readonly CombatTurnManager _combatTurnManager = new();
    private readonly CombatUiManager _combatUiManager = new();

    public override void _Ready()
    {
        _combatUiManager.SetNextButton(GetNode<Button>("../Button"));
        _combatTurnManager.InitializePlayers(this);
        _combatTurnManager.SetInitialAlliesAndEnemies(GetChildren().OfType<Player>());
        _combatUiManager.InitPlayerUi(
            _combatTurnManager.CurrentPlayer,
            _combatTurnManager.FirstAlly,
            _combatTurnManager.FirstEnemy
        );
        if (!_combatTurnManager.CurrentPlayer.IsAlly)
        {
            InitEnemyAi();
        }
    }

    public void NextTurn()
    {
        Player currentPlayer = _combatTurnManager.CurrentPlayer;
        // Disconnect the event handler from current player if it's an enemy
        if (!currentPlayer.IsAlly)
        {
            currentPlayer.EnemyFinishedTurn -= OnEnemyFinishedTurn;
        }

        // Get the next player
        Player nextPlayer = _combatTurnManager.GetNextPlayer();

        // Handle current player's exit
        _combatUiManager.HandleCurrentAllyHandExit(currentPlayer, nextPlayer);


        // Hide lastAlly hand to swap with nextAlly hand as it gets shown
        if (!currentPlayer.IsAlly && nextPlayer.IsAlly)
        {
            Player lastAlly = _combatTurnManager.GetLastAlly();
            _combatUiManager.HideLastAllyHand(lastAlly);
        }

        // Update and handle enemy board transitions
        _combatUiManager.HideLastPlayerBoard(nextPlayer.IsAlly);

        // End current players turn and start next players turn
        currentPlayer = _combatTurnManager.SwapTurnToNextPlayer(nextPlayer);

        // Handle new player's entrance
        if (!currentPlayer.IsAlly)
        {
            currentPlayer.EnemyFinishedTurn += OnEnemyFinishedTurn;
        }

        // Handle UI turn change
        _combatUiManager.ShowPlayerUi(currentPlayer);

        currentPlayer.StartTurn();
    }

    private void InitEnemyAi()
    {
        GetTree().CreateTimer(1).Timeout += _combatTurnManager.CurrentPlayer.EnemyPlayHand;
        _combatTurnManager.CurrentPlayer.EnemyFinishedTurn += OnEnemyFinishedTurn;
    }

    private void OnEnemyFinishedTurn()
    {
        GetTree().CreateTimer(1.5).Timeout += NextTurn;
    }
}
