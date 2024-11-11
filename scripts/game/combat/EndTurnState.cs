using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class EndTurnState : ICombatState
{
    private readonly Player _player;
    private readonly TurnManager _turnManager;
    private readonly CombatUiManager _uiManager;
    private bool _isProcessingEndTurn;

    public EndTurnState(TurnManager turnManager, Player player, CombatUiManager uiManager)
    {
        _turnManager = turnManager;
        _player = player;
        _uiManager = uiManager;
    }

    public void Enter()
    {
        if (_isProcessingEndTurn)
        {
            return; // Prevent recursive entry
        }

        _isProcessingEndTurn = true;
        ProcessEndTurn();
    }

    public void Exit()
    {
        _isProcessingEndTurn = false;

        // Cleanup any remaining subscriptions
        if (!_player.IsAlly)
        {
            _player.EnemyFinishedTurn -= OnEnemyFinishedTurn;
        }
    }

    public void Update() { }

    public bool CanTransitionTo(ICombatState nextState)
    {
        return nextState is StartTurnState && !_isProcessingEndTurn;
    }

    private void ProcessEndTurn()
    {
        Player nextPlayer = _turnManager.GetNextPlayer();

        // Handle current player's exit
        HandleCurrentPlayerExit(nextPlayer);

        // Handle alliance-specific transitions
        HandleAllianceTransitions(nextPlayer);

        // Update board visibility
        _uiManager.HideLastPlayerBoard(nextPlayer.IsAlly);

        // Perform the actual turn swap
        Player newCurrentPlayer = _turnManager.SwapTurnToNextPlayer(nextPlayer);

        // After turn swap, start the next turn after a short delay
        _player.GetTree().CreateTimer(0.5).Timeout += () =>
        {
            _isProcessingEndTurn = false;
            newCurrentPlayer.StartTurn();
            _turnManager.TransitionTo<StartTurnState>();
        };
    }

    private void HandleCurrentPlayerExit(Player nextPlayer)
    {
        if (_player.IsAlly && !nextPlayer.IsAlly)
        {
            CombatUiManager.ShowHand(_player, true, true);
        }
        else
        {
            CombatUiManager.ShowHand(_player, false);
        }

        _uiManager.HandleCurrentAllyHandExit(_player, nextPlayer);
    }

    private void HandleAllianceTransitions(Player nextPlayer)
    {
        if (!_player.IsAlly && nextPlayer.IsAlly)
        {
            Player lastAlly = _turnManager.GetLastAlly();
            CombatUiManager.HideLastAllyHand(lastAlly);
        }
    }

    // private void SetupNextPlayerTurn()
    // {
    //     // Show UI for next player
    //     _uiManager.ShowPlayerUi(_player);
    //
    //     if (!_player.IsAlly)
    //     {
    //         _player.EnemyFinishedTurn += OnEnemyFinishedTurn;
    //     }
    //
    //     GetTree().CreateTimer(0.5).Timeout += () =>
    //     {
    //         _isProcessingEndTurn = false;
    //         _turnManager.TransitionTo<StartTurnState>();
    //     };
    // }

    private void OnEnemyFinishedTurn()
    {
        _player.GetTree().CreateTimer(1.5).Timeout += () =>
        {
            if (!_isProcessingEndTurn)
            {
                _turnManager.TransitionTo<StartTurnState>();
            }
        };
    }
}
