using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class StartTurnState : ICombatState
{
    private readonly Player _player;
    private readonly TurnManager _turnManager;
    private readonly CombatUiManager _uiManager;

    public StartTurnState(TurnManager turnManager, Player player, CombatUiManager uiManager)
    {
        _turnManager = turnManager;
        _player = player;
        _uiManager = uiManager;
    }

    public void Enter()
    {
        _player.TurnIndicator.Visible = true;

        // Show appropriate boards
        Player firstEnemy = _turnManager.FirstEnemy;
        Player firstAlly = _turnManager.FirstAlly;

        if (_player.IsAlly)
        {
            // Show the player's UI elements and enable buttons
            _uiManager.SetAttackButtonState(true);
            _uiManager.SetEndTurnButtonState(true);
            // Show the ally's hand and board
            CombatUiManager.ShowHand(_player, true);
            CombatUiManager.ShowBoard(_player, true);
        }
        else
        {
            // For enemy turns, show the first ally's hand (disabled) and board
            CombatUiManager.ShowHand(firstAlly, true, true);
            CombatUiManager.ShowBoard(firstAlly, true);
            // Initialize enemy AI
            InitEnemyAi();
        }

        // Always show the first enemy's board
        CombatUiManager.ShowBoard(firstEnemy, true);

        _turnManager.TransitionTo<DrawPhaseState>();
    }

    public void Exit()
    {
        // Clean up any event subscriptions if necessary
        if (!_player.IsAlly)
        {
            _player.EnemyFinishedTurn -= OnEnemyFinishedTurn;
        }
    }

    public void Update() { }

    public bool CanTransitionTo(ICombatState nextState)
    {
        return nextState is DrawPhaseState;
    }

    private void InitEnemyAi()
    {
        _player.GetTree().CreateTimer(1).Timeout += _player.EnemyPlayHand;
        _player.EnemyFinishedTurn += OnEnemyFinishedTurn;
    }

    private void OnEnemyFinishedTurn()
    {
        _player.GetTree().CreateTimer(1.5).Timeout +=
            () => _turnManager.SwapTurnToNextPlayer(_turnManager.GetNextPlayer());
    }
}
