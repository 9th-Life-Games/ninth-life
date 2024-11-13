using System;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class AttackMode : ICombatMode
{
    private readonly CombatManager _combatManager;
    private readonly bool _shouldLog = true;
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

    public void Enter(bool isForward = true, Player playerToPreview = null)
    {
        Logger.Debug("Entering Attack Mode", _shouldLog);
        if (!isForward)
        {
            // Create a Random instance
            Random random = new();

            // Generate a random index between 0 and the number of allies minus 1
            int randomAllyIndex = random.Next(0, _turnManager.AllyTurnOrder.Count);
            Player targetPlayer = _turnManager.AllyTurnOrder[randomAllyIndex];

            TargetPlayer = targetPlayer;
            Logger.Debug($"Initial Target Ally: {TargetPlayer.PlayerName}", _shouldLog);

            Logger.Debug("Enemy attacking, skipping setup", _shouldLog);
            return;
        }

        Logger.Debug("Setting up player attack mode", _shouldLog);
        _uiManager.SetNextButtonState(false);
        _turnManager.EnemyTurnOrder.ForEach(player =>
        {
            Logger.Debug($"Setting up attack indicators for: {player.PlayerName}", _shouldLog);
            player.SetAttackModeIndicator(true);
            player.PlayerClicked += OnEnemyClicked;
            player.PlayerMouseHoveredIn += OnEnemyMouseHoveredIn;
        });

        // Set initial target
        _targetEnemyIndex = 0;
        TargetPlayer = _turnManager.EnemyTurnOrder[0];
        Logger.Debug($"Initial Target Enemy: {TargetPlayer.PlayerName}", _shouldLog);
        TargetPlayer.SetSelectedEnemy(true);

        Logger.Debug($"Showing initial enemy board for: {TargetPlayer.PlayerName}", _shouldLog);
        ShowEnemyBoard(TargetPlayer);

        Logger.Debug("Disabling current player's hand", _shouldLog);
        _turnManager.CurrentPlayer.SlideHandDisabled();
    }

    public void Exit()
    {
        Logger.Debug("Exiting Attack Mode", _shouldLog);
        Player firstEnemy = _combatManager.FirstEnemy;
        _uiManager.SetNextButtonState(true);

        _turnManager.EnemyTurnOrder.ForEach(player =>
        {
            Logger.Debug($"Cleaning up attack indicators for: {player.PlayerName}", _shouldLog);
            player.SetSelectedEnemy(false);
            player.SetAttackModeIndicator(false);
        });

        if (!_turnManager.CurrentPlayer.IsAlly)
        {
            Logger.Debug("Skipping stuff because it's an enemy", _shouldLog);
            TargetPlayer = null;
            return;
        }

        Player currentlyShownEnemy = _uiManager.LastEnemy ?? firstEnemy;
        Logger.Debug($"Hiding current enemy board: {currentlyShownEnemy.PlayerName}", _shouldLog);
        CombatUiManager.ShowBoard(currentlyShownEnemy, false);

        Player boardToShow = _uiManager.LastEnemy ?? firstEnemy;
        Logger.Debug($"Showing final enemy board: {boardToShow.PlayerName}", _shouldLog);
        CombatUiManager.ShowBoard(boardToShow, true);

        if (_turnManager.CurrentPlayer.CurrentCardPlays < _turnManager.CurrentPlayer.TotalCardPlays &&
            _turnManager.CurrentPlayer.IsAlly)
        {
            Logger.Debug("Re-enabling player hand", _shouldLog);
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

    private void ShowEnemyBoard(Player newTarget)
    {
        Logger.Debug($"Showing enemy board for: {newTarget.PlayerName}", _shouldLog);
        Player firstEnemy = _combatManager.FirstEnemy;

        if (_uiManager.LastEnemy != null)
        {
            Logger.Debug($"Hiding last enemy board: {_uiManager.LastEnemy.PlayerName}", _shouldLog);
            CombatUiManager.ShowBoard(_uiManager.LastEnemy, false);
        }
        else
        {
            Logger.Debug($"Hiding first enemy board: {firstEnemy.PlayerName}", _shouldLog);
            CombatUiManager.ShowBoard(firstEnemy, false);
        }

        Logger.Debug($"Setting LastEnemy to: {newTarget.PlayerName}", _shouldLog);
        _uiManager.SetLastEnemy(newTarget);
        CombatUiManager.ShowBoard(newTarget, true);
    }

    private void OnEnemyClicked(Player player)
    {
        Logger.Debug($"Enemy clicked: {player.PlayerName}", _shouldLog);
        ExecuteAttack();
    }

    private void OnEnemyMouseHoveredIn(Player player)
    {
        Logger.Debug($"Mouse entered enemy: {player.PlayerName}", _shouldLog);
        if (player == TargetPlayer)
        {
            Logger.Debug("Skipping - already targeting this enemy", _shouldLog);
            return;
        }

        Logger.Debug($"Switching target from {TargetPlayer.PlayerName} to {player.PlayerName}", _shouldLog);
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
            Logger.Debug("No target selected, skipping attack", _shouldLog);
            return;
        }

        Logger.Debug($"Executing attack on: {TargetPlayer.PlayerName}", _shouldLog);
        _turnManager.EnemyTurnOrder.ForEach(player =>
        {
            Logger.Debug($"Removing click handlers from: {player.PlayerName}", _shouldLog);
            player.PlayerClicked -= OnEnemyClicked;
            player.PlayerMouseHoveredIn -= OnEnemyMouseHoveredIn;
        });

        _combatManager.AttackSound.Play();

        Logger.Debug($"Updating LastEnemy to: {TargetPlayer.PlayerName}", _shouldLog);
        _uiManager.SetLastEnemy(TargetPlayer);
        ShowEnemyBoard(TargetPlayer);

        Logger.Debug("Initiating combat manager attack", _shouldLog);
        _combatManager.ExecuteAttack(TargetPlayer);
    }

    private void CycleTargetForward()
    {
        Logger.Debug($"Cycling target forward from: {TargetPlayer.PlayerName}", _shouldLog);
        TargetPlayer.SetSelectedEnemy(false);
        _targetEnemyIndex = (_targetEnemyIndex + 1) % _turnManager.EnemyTurnOrder.Count;
        Player nextTarget = _turnManager.EnemyTurnOrder[_targetEnemyIndex];

        Logger.Debug($"New target: {nextTarget.PlayerName}", _shouldLog);
        ShowEnemyBoard(nextTarget);

        TargetPlayer = nextTarget;
        TargetPlayer.SetSelectedEnemy(true);
    }

    private void CycleTargetBackward()
    {
        Logger.Debug($"Cycling target backward from: {TargetPlayer.PlayerName}", _shouldLog);
        TargetPlayer.SetSelectedEnemy(false);
        _targetEnemyIndex--;
        if (_targetEnemyIndex < 0)
        {
            _targetEnemyIndex = _turnManager.EnemyTurnOrder.Count - 1;
        }

        Player nextTarget = _turnManager.EnemyTurnOrder[_targetEnemyIndex];

        Logger.Debug($"New target: {nextTarget.PlayerName}", _shouldLog);
        ShowEnemyBoard(nextTarget);

        TargetPlayer = nextTarget;
        TargetPlayer.SetSelectedEnemy(true);
    }
}
