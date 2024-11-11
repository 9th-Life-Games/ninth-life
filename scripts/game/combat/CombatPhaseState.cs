using System;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class CombatPhaseState : ICombatState
{
    private readonly Node2D _defenseDraw;
    private readonly Player _player;
    private readonly Node2D _rollIndicator;
    private readonly TurnManager _turnManager;
    private readonly CombatUiManager _uiManager;
    private int _attackRoll;
    private bool _isResolvingCombat;
    private int _targetEnemyIndex;
    public Player CurrentTarget;

    public CombatPhaseState(
        TurnManager turnManager,
        Player player,
        CombatUiManager uiManager,
        Node2D rollIndicator,
        Node2D defenseDraw)
    {
        _turnManager = turnManager;
        _player = player;
        _uiManager = uiManager;
        _rollIndicator = rollIndicator;
        _defenseDraw = defenseDraw;
    }

    public bool IsInAttackMode { get; private set; }

    public void Enter()
    {
        if (!_player.IsAlly)
        {
            // AI combat logic
            SelectRandomTarget();
            ResolveCombat();
        }
        else
        {
            EnterAttackMode();
        }
    }

    public void Exit()
    {
        DisableEnemySelection();
        _isResolvingCombat = false;
        IsInAttackMode = false;
        CurrentTarget = null;

        // Re-enable hand if player still has card plays
        if (_player.CurrentCardPlays < _player.TotalCardPlays)
        {
            CombatUiManager.ShowHand(_player, true);
        }
    }

    public void Update() { }

    public bool CanTransitionTo(ICombatState nextState)
    {
        return nextState is EndTurnState && !_isResolvingCombat;
    }

    private void EnterAttackMode()
    {
        IsInAttackMode = true;
        _uiManager.SetEndTurnButtonState(false);
        EnableEnemySelection();
        _player.SlideHandDisabled();

        // Select first enemy by default
        _targetEnemyIndex = 0;
        CurrentTarget = _turnManager.EnemyTurnOrder[0];
        CurrentTarget.SetSelectedEnemy(true);
    }

    public void CycleTargetForward()
    {
        if (!IsInAttackMode)
        {
            return;
        }

        CurrentTarget.SetSelectedEnemy(false);

        if (_targetEnemyIndex >= _turnManager.EnemyTurnOrder.Count - 1)
        {
            _targetEnemyIndex = 0;
        }
        else
        {
            _targetEnemyIndex++;
        }

        CurrentTarget = _turnManager.EnemyTurnOrder[_targetEnemyIndex];
        CurrentTarget.SetSelectedEnemy(true);
    }

    public void CycleTargetBackward()
    {
        if (!IsInAttackMode)
        {
            return;
        }

        CurrentTarget.SetSelectedEnemy(false);

        if (_targetEnemyIndex <= 0)
        {
            _targetEnemyIndex = _turnManager.EnemyTurnOrder.Count - 1;
        }
        else
        {
            _targetEnemyIndex--;
        }

        CurrentTarget = _turnManager.EnemyTurnOrder[_targetEnemyIndex];
        CurrentTarget.SetSelectedEnemy(true);
    }

    private void EnableEnemySelection()
    {
        foreach (Player enemy in _turnManager.EnemyTurnOrder)
        {
            enemy.SetAttackModeIndicator(true);
        }
    }

    private void DisableEnemySelection()
    {
        foreach (Player enemy in _turnManager.EnemyTurnOrder)
        {
            enemy.SetAttackModeIndicator(false);
            enemy.SetSelectedEnemy(false);
        }
    }

    public void ConfirmAttack()
    {
        if (!IsInAttackMode || _isResolvingCombat)
        {
            return;
        }

        IsInAttackMode = false;
        _isResolvingCombat = true;
        _uiManager.SetAttackButtonState(false);

        ResolveCombat();
    }

    private void SelectRandomTarget()
    {
        Random random = new();
        _targetEnemyIndex = random.Next(_turnManager.AllyTurnOrder.Count);
        CurrentTarget = _turnManager.AllyTurnOrder[_targetEnemyIndex];
    }

    private void ResolveCombat()
    {
        RollForAttack();
        CurrentTarget.DrawCardForDefense(() =>
        {
            _isResolvingCombat = false;
            _uiManager.SetEndTurnButtonState(true);

            if (_player.CurrentCardPlays < _player.TotalCardPlays)
            {
                CombatUiManager.ShowHand(_player, true);
            }

            _turnManager.TransitionTo<EndTurnState>();
        });
    }

    private void RollForAttack()
    {
        Random random = new();
        _attackRoll = random.Next(1, 11) + _player.MasteryBonus;

        // Update roll indicator UI
        _rollIndicator.GetNode<Label>("Label").Text = $"{_attackRoll}";
        _rollIndicator.GetNode<AnimationPlayer>("AnimationPlayer").Play("show_result");
    }
}
