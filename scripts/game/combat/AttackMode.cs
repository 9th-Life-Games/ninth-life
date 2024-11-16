using System;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class AttackMode : ICombatMode
{
    private readonly CombatManager _combatManager;
    private readonly Player _firstEnemy;
    private readonly bool _shouldLog = true;
    private readonly CombatTurnManager _turnManager;
    private readonly CombatUiManager _uiManager;
    private int _targetEnemyIndex;

    public AttackMode(CombatManager combatManager, CombatUiManager uiManager, CombatTurnManager turnManager)
    {
        _combatManager = combatManager;
        _uiManager = uiManager;
        _turnManager = turnManager;
        _firstEnemy = combatManager.FirstEnemy;
    }

    public Player TargetPlayer { get; private set; }

    public void Enter(bool isForward = true, Player playerToPreview = null)
    {
        Logger.Debug("AttackMode: Entering Attack Mode", _shouldLog);

        if (!isForward)
        {
            TargetPlayer = SelectRandomAllyTarget();
            Logger.Debug($"AttackMode: Initial Target Ally: {TargetPlayer.PlayerName}", _shouldLog);
            return;
        }

        Logger.Debug("AttackMode: Setting up player attack mode", _shouldLog);
        _uiManager.SetEndTurnButtonState(false);
        SetupEnemyTargetables();

        // Set initial target
        _targetEnemyIndex = 0;
        TargetPlayer = _turnManager.EnemyTurnOrder[0];
        Logger.Debug($"AttackMode: Initial Target Enemy: {TargetPlayer.PlayerName}", _shouldLog);
        TargetPlayer.SetSelectedEnemy(true);

        Logger.Debug($"AttackMode: Showing initial enemy board for: {TargetPlayer.PlayerName}", _shouldLog);
        ShowEnemyBoard(TargetPlayer);

        Logger.Debug("AttackMode: Disabling current player's hand", _shouldLog);
        _turnManager.CurrentPlayer.SlideHandDisabled();
    }

    public void Exit()
    {
        Logger.Debug("AttackMode: Exiting Attack Mode", _shouldLog);

        if (!_turnManager.CurrentPlayer.IsAlly)
        {
            Logger.Debug("AttackMode: Skipping board management for enemy turn", _shouldLog);
            TargetPlayer = null;
            return;
        }

        CleanupEnemyTargetables();

        Player currentlyShownEnemy = _uiManager.LastEnemy ?? _firstEnemy;
        Logger.Debug($"AttackMode: Hiding current enemy board: {currentlyShownEnemy.PlayerName}", _shouldLog);
        CombatUiManager.ShowBoard(currentlyShownEnemy, false);

        Player boardToShow = _uiManager.LastEnemy ?? _firstEnemy;
        Logger.Debug($"AttackMode: Showing final enemy board: {boardToShow.PlayerName}", _shouldLog);
        CombatUiManager.ShowBoard(boardToShow, true);

        if (_turnManager.CurrentPlayer.CurrentCardPlays < _turnManager.CurrentPlayer.TotalCardPlays)
        {
            Logger.Debug("AttackMode: Re-enabling player hand", _shouldLog);
            CombatUiManager.ShowHand(_turnManager.CurrentPlayer, true);
        }

        TargetPlayer = null;
    }

    public void Update(InputEvent inputEvent)
    {
        if (!_turnManager.CurrentPlayer.IsAlly)
        {
            return;
        }

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
            Logger.Debug($"AttackMode: Is Ally: {_turnManager.CurrentPlayer.IsAlly}");
            CycleTargetForward();
        }
    }

    private Player SelectRandomAllyTarget()
    {
        Random random = new();
        int randomAllyIndex = random.Next(0, _turnManager.AllyTurnOrder.Count);
        return _turnManager.AllyTurnOrder[randomAllyIndex];
    }

    private void SetupEnemyTargetables()
    {
        _turnManager.EnemyTurnOrder.ForEach(player =>
        {
            Logger.Debug($"AttackMode: Setting up attack indicators for: {player.PlayerName}", _shouldLog);
            player.SetAttackModeIndicator(true);
            player.PlayerClicked += OnEnemyClicked;
            player.PlayerMouseHoveredIn += OnEnemyMouseHoveredIn;
        });
    }

    private void CleanupEnemyTargetables()
    {
        _turnManager.EnemyTurnOrder.ForEach(player =>
        {
            Logger.Debug($"AttackMode: Cleaning up attack indicators for: {player.PlayerName}", _shouldLog);
            player.SetSelectedEnemy(false);
            player.SetAttackModeIndicator(false);
            player.PlayerClicked -= OnEnemyClicked;
            player.PlayerMouseHoveredIn -= OnEnemyMouseHoveredIn;
        });
    }

    private void ShowEnemyBoard(Player newTarget)
    {
        Logger.Debug($"AttackMode: Showing enemy board for: {newTarget.PlayerName}", _shouldLog);

        if (_uiManager.LastEnemy != null)
        {
            Logger.Debug($"AttackMode: Hiding last enemy board: {_uiManager.LastEnemy.PlayerName}", _shouldLog);
            CombatUiManager.ShowBoard(_uiManager.LastEnemy, false);
        }
        else
        {
            Logger.Debug($"AttackMode: Hiding first enemy board: {_firstEnemy.PlayerName}", _shouldLog);
            CombatUiManager.ShowBoard(_firstEnemy, false);
        }

        Logger.Debug($"AttackMode: Setting LastEnemy to: {newTarget.PlayerName}", _shouldLog);
        _uiManager.SetLastEnemy(newTarget);
        CombatUiManager.ShowBoard(newTarget, true);
    }

    private void OnEnemyClicked(Player player)
    {
        Logger.Debug($"AttackMode: Enemy clicked: {player.PlayerName}", _shouldLog);
        ExecuteAttack();
    }

    private void OnEnemyMouseHoveredIn(Player player)
    {
        Logger.Debug($"AttackMode: Mouse entered enemy: {player.PlayerName}", _shouldLog);
        if (player == TargetPlayer)
        {
            Logger.Debug("AttackMode: Skipping - already targeting this enemy", _shouldLog);
            return;
        }

        Logger.Debug($"AttackMode: Switching target from {TargetPlayer.PlayerName} to {player.PlayerName}", _shouldLog);
        TargetPlayer.SetSelectedEnemy(false);
        _targetEnemyIndex = _turnManager.GetPlayerIndex(player);
        TargetPlayer = player;
        TargetPlayer.SetSelectedEnemy(true);

        ShowEnemyBoard(player);
    }

    private void ExecuteAttack()
    {
        if (TargetPlayer == null)
        {
            Logger.Debug("AttackMode: No target selected, skipping attack", _shouldLog);
            return;
        }

        Logger.Debug($"AttackMode: Executing attack on: {TargetPlayer.PlayerName}", _shouldLog);
        _turnManager.EnemyTurnOrder.ForEach(player =>
        {
            Logger.Debug($"AttackMode: Removing click handlers from: {player.PlayerName}", _shouldLog);
            player.PlayerClicked -= OnEnemyClicked;
            player.PlayerMouseHoveredIn -= OnEnemyMouseHoveredIn;
        });

        // _combatManager.AttackSound.Play();

        Logger.Debug($"AttackMode: Updating LastEnemy to: {TargetPlayer.PlayerName}", _shouldLog);
        _uiManager.SetLastEnemy(TargetPlayer);
        ShowEnemyBoard(TargetPlayer);

        Logger.Debug("AttackMode: Initiating combat manager attack", _shouldLog);
        _combatManager.ExecuteAttack(TargetPlayer);
    }

    private void CycleTargetForward()
    {
        Logger.Debug($"******AttackMode: Cycling target forward from: {TargetPlayer.PlayerName}", _shouldLog);
        TargetPlayer.SetSelectedEnemy(false);
        _targetEnemyIndex = (_targetEnemyIndex + 1) % _turnManager.EnemyTurnOrder.Count;
        Player nextTarget = _turnManager.EnemyTurnOrder[_targetEnemyIndex];

        Logger.Debug($"AttackMode: New target: {nextTarget.PlayerName}", _shouldLog);
        ShowEnemyBoard(nextTarget);

        TargetPlayer = nextTarget;
        TargetPlayer.SetSelectedEnemy(true);
    }

    private void CycleTargetBackward()
    {
        Logger.Debug($"AttackMode: Cycling target backward from: {TargetPlayer.PlayerName}", _shouldLog);
        TargetPlayer.SetSelectedEnemy(false);
        _targetEnemyIndex--;
        if (_targetEnemyIndex < 0)
        {
            _targetEnemyIndex = _turnManager.EnemyTurnOrder.Count - 1;
        }

        Player nextTarget = _turnManager.EnemyTurnOrder[_targetEnemyIndex];

        Logger.Debug($"AttackMode: New target: {nextTarget.PlayerName}", _shouldLog);
        ShowEnemyBoard(nextTarget);

        TargetPlayer = nextTarget;
        TargetPlayer.SetSelectedEnemy(true);
    }
}
