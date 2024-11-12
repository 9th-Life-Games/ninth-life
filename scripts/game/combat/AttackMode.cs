using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class AttackMode : ICombatMode
{
    private readonly CombatManager _combatManager;
    private readonly CombatTurnManager _turnManager;
    private readonly CombatUiManager _uiManager;
    private int _targetEnemyIndex;

    public AttackMode(CombatManager combatManager, CombatUiManager uiManager, CombatTurnManager turnManager)
    {
        _combatManager = combatManager;
        _uiManager = uiManager;
        _turnManager = turnManager;
    }

    public Player TargetPlayer { get; private set; }

    public void Enter(bool isForward = true)
    {
        _uiManager.SetNextButtonState(false);
        _turnManager.EnemyTurnOrder.ForEach(player => player.SetAttackModeIndicator(true));

        // Set initial target
        _targetEnemyIndex = 0;
        TargetPlayer = _turnManager.EnemyTurnOrder[0];
        Logger.Debug($"Target Enemy: {TargetPlayer.PlayerName}");
        TargetPlayer.SetSelectedEnemy(true);

        _turnManager.CurrentPlayer.SlideHandDisabled();
    }

    public void Exit()
    {
        _uiManager.SetNextButtonState(true);
        _turnManager.EnemyTurnOrder.ForEach(player =>
        {
            player.SetSelectedEnemy(false);
            player.SetAttackModeIndicator(false);
        });

        if (_turnManager.CurrentPlayer.CurrentCardPlays < _turnManager.CurrentPlayer.TotalCardPlays)
        {
            CombatUiManager.ShowHand(_turnManager.CurrentPlayer, true);
        }

        TargetPlayer = null;
    }

    public void Update(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed("accept"))
        {
            ExecuteAttack();
        }
        else if (inputEvent.IsActionPressed("previous_preview"))
        {
            CycleTargetBackward();
        }
        else if (inputEvent.IsActionPressed("next_preview"))
        {
            CycleTargetForward();
        }
    }

    private void ExecuteAttack()
    {
        if (TargetPlayer == null)
        {
            return;
        }

        _combatManager.ExecuteAttack(TargetPlayer);
        // _combatManager.ExitCurrentMode();
    }

    private void CycleTargetForward()
    {
        TargetPlayer.SetSelectedEnemy(false);
        _targetEnemyIndex = (_targetEnemyIndex + 1) % _turnManager.EnemyTurnOrder.Count;
        TargetPlayer = _turnManager.EnemyTurnOrder[_targetEnemyIndex];
        TargetPlayer.SetSelectedEnemy(true);
    }

    private void CycleTargetBackward()
    {
        TargetPlayer.SetSelectedEnemy(false);
        _targetEnemyIndex--;
        if (_targetEnemyIndex < 0)
        {
            _targetEnemyIndex = _turnManager.EnemyTurnOrder.Count - 1;
        }

        TargetPlayer = _turnManager.EnemyTurnOrder[_targetEnemyIndex];
        TargetPlayer.SetSelectedEnemy(true);
    }
}
