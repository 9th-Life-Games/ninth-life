using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class DrawPhaseState : ICombatState
{
    // private const int HandSize = 4; // Changed to match Player's constant
    private readonly Player _player;
    private readonly TurnManager _turnManager;
    private readonly CombatUiManager _uiManager;
    private bool _isDrawing;

    public DrawPhaseState(TurnManager turnManager, Player player, CombatUiManager uiManager)
    {
        _turnManager = turnManager;
        _player = player;
        _uiManager = uiManager;
    }

    public void Enter()
    {
        _isDrawing = true;
        _player.DisablePlayerHand();
        int amountToDraw = _player.HandSize - _player.Hand.GetChildren().Count;

        // Subscribe to the PlayerFinishedDrawing event
        _player.PlayerFinishedDrawing += OnPlayerFinishedDrawing;

        // Use the Player's existing DrawCards functionality
        _player.DrawCards(amountToDraw, 0.5);
    }

    public void Exit()
    {
        _isDrawing = false;
        _player.PlayerFinishedDrawing -= OnPlayerFinishedDrawing;
    }

    public void Update() { }

    public bool CanTransitionTo(ICombatState nextState)
    {
        return nextState is MainPhaseState && !_isDrawing;
    }

    private void OnPlayerFinishedDrawing()
    {
        _isDrawing = false;

        if (!_player.IsAlly)
        {
            _player.EnemyPlayHand();
        }
        else
        {
            _player.EnablePlayerHand();
            _uiManager.SetAttackButtonState(true);
            _uiManager.SetEndTurnButtonState(true);
        }

        _turnManager.TransitionTo<MainPhaseState>();
    }
}
