using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class MainPhaseState : ICombatState
{
    private readonly Player _player;
    private readonly TurnManager _turnManager;
    private readonly CombatUiManager _uiManager;
    private bool _isProcessingAction;

    public MainPhaseState(TurnManager turnManager, Player player, CombatUiManager uiManager)
    {
        _turnManager = turnManager;
        _player = player;
        _uiManager = uiManager;
    }

    public void Enter()
    {
        if (_player.IsAlly)
        {
            // Set up ally's turn
            SetupAllyTurn();
        }
        else
        {
            // Set up enemy's turn
            SetupEnemyTurn();
        }
    }

    public void Exit()
    {
        // Clean up subscriptions
        if (_player.IsAlly)
        {
            _player.AllyHandEnabled -= OnAllyHandEnabled;
        }
        else
        {
            _player.EnemyFinishedTurn -= OnEnemyFinishedTurn;
        }

        // Clean up UI
        if (!_player.IsAlly)
        {
            CombatUiManager.ShowHand(_player, false);
        }
    }

    public void Update()
    {
        // Most state changes are handled via events now
        // This could be used for additional checks or animations
    }

    public bool CanTransitionTo(ICombatState nextState)
    {
        // Can only transition if we're not in the middle of processing an action
        if (_isProcessingAction)
        {
            return false;
        }

        return nextState switch
        {
            EndTurnState => true,
            CombatPhaseState => _player.IsAlly && _player.CurrentCardPlays >= _player.TotalCardPlays,
            _ => false
        };
    }

    private void SetupAllyTurn()
    {
        _isProcessingAction = false;
        CombatUiManager.ShowHand(_player, true);
        _uiManager.SetAttackButtonState(true);
        _uiManager.SetEndTurnButtonState(true);

        // Subscribe to relevant events
        _player.AllyHandEnabled += OnAllyHandEnabled;
    }

    private void SetupEnemyTurn()
    {
        _isProcessingAction = true;
        _player.EnemyFinishedTurn += OnEnemyFinishedTurn;
        _player.GetTree().CreateTimer(1.0).Timeout += () =>
        {
            _player.EnemyPlayHand();
        };
    }

    private void OnAllyHandEnabled(bool enabled)
    {
        if (!enabled && _player.CurrentCardPlays >= _player.TotalCardPlays)
        {
            _isProcessingAction = true;
            _turnManager.TransitionTo<EndTurnState>();
        }
    }

    private void OnEnemyFinishedTurn()
    {
        _isProcessingAction = true;
        _turnManager.TransitionTo<EndTurnState>();
    }
}
