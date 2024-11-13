using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class PreviewMode : ICombatMode
{
    private readonly CombatManager _combatManager;
    private readonly Player _firstEnemy;
    private readonly bool _shouldLog;
    private readonly CombatTurnManager _turnManager;
    private readonly CombatUiManager _uiManager;
    private Player _previewedPlayer;
    private int _previewIndex = -1;
    private Player _previouslyShownPlayer;

    public PreviewMode(CombatManager combatManager, CombatUiManager uiManager, CombatTurnManager turnManager)
    {
        _combatManager = combatManager;
        _uiManager = uiManager;
        _turnManager = turnManager;
        _firstEnemy = combatManager.FirstEnemy;
    }

    public void Enter(bool isForward = true, Player playerToPreview = null)
    {
        _previewIndex = _turnManager.GetCurrentPlayerIndex();
        _uiManager.SetNextButtonState(false);
        _uiManager.SetAttackButtonState(false);
        _turnManager.CurrentPlayer.TurnIndicatorPreviewColor(true);

        foreach (Player player in _turnManager.TurnOrder)
        {
            player.PlayerClicked += OnPlayerClicked;
        }

        // Show initial preview based on direction
        if (playerToPreview != null)
        {
            _previewIndex = _turnManager.GetPlayerIndex(playerToPreview);
            ShowPreview(playerToPreview);
        }
        else
        {
            // Initialize preview index based on direction
            _previewIndex = isForward
                ? _turnManager.GetCurrentPlayerIndex() + 1
                : _turnManager.GetTurnOrderCount() - 1;

            // Handle wraparound for forward direction
            if (_previewIndex >= _turnManager.GetTurnOrderCount())
            {
                _previewIndex = 0;
            }

            ShowPreview(_turnManager.GetPlayerToPreview(_previewIndex));
        }
    }

    public void Exit()
    {
        Logger.Debug("****Removing listeners", _shouldLog);
        foreach (Player player in _turnManager.TurnOrder)
        {
            player.PlayerClicked -= OnPlayerClicked;
        }
    }

    public void Update(InputEvent inputEvent)
    {
        if (!_turnManager.CurrentPlayer.IsAlly)
        {
            return;
        }

        if (inputEvent.IsActionPressed("previous_preview"))
        {
            CyclePreviewBackward();
        }
        else if (inputEvent.IsActionPressed("next_preview"))
        {
            CyclePreviewForward();
        }
    }

    private void OnPlayerClicked(Player player)
    {
        _previewIndex = _turnManager.GetPlayerIndex(player);
        ShowPreview(player);
    }

    private void CyclePreviewForward()
    {
        Logger.Debug($"***Current preview index {_previewIndex}", _shouldLog);
        if (_previewIndex == -1)
        {
            Logger.Debug("***CyclePreviewForward -1: setting index back to start", _shouldLog);
            _previewIndex = _turnManager.GetCurrentPlayerIndex();
        }

        _previewIndex++;
        if (_previewIndex > _turnManager.GetTurnOrderCount() - 1)
        {
            Logger.Debug("***Preview > player count: setting index back to start", _shouldLog);
            _previewIndex = _turnManager.GetCurrentPlayerIndex();
        }

        Logger.Debug($"***Next preview index: {_previewIndex}", _shouldLog);
        ShowPreview(_turnManager.GetPlayerToPreview(_previewIndex));
    }

    private void CyclePreviewBackward()
    {
        if (_previewIndex == -1)
        {
            _previewIndex = _turnManager.GetCurrentPlayerIndex();
        }

        _previewIndex--;
        if (_previewIndex < 0)
        {
            _previewIndex = _turnManager.GetTurnOrderCount() - 1;
        }

        Logger.Debug($"***Previous preview index: {_previewIndex}", _shouldLog);
        ShowPreview(_turnManager.GetPlayerToPreview(_previewIndex));
    }

    private void ShowPreview(Player playerToPreview)
    {
        Player currentPlayer = _turnManager.CurrentPlayer;
        Player lastEnemy = _uiManager.LastEnemy;
        Logger.Debug("Starting Preview", _shouldLog);

        // If previewing current player, end preview instead
        if (playerToPreview == currentPlayer)
        {
            Logger.Debug("***Preview is the same as the current player", _shouldLog);
            EndPreview(currentPlayer, playerToPreview);
            return;
        }

        EndPreview(currentPlayer, playerToPreview);

        Logger.Debug($"Start: [1] Showing player to preview: {playerToPreview.PlayerName}'s board", _shouldLog);
        CombatUiManager.ShowBoard(playerToPreview, true);
        if (_previouslyShownPlayer == null && lastEnemy == null && !playerToPreview.IsAlly &&
            playerToPreview != _firstEnemy)
        {
            Logger.Debug("Start: *Hiding first enemy board", _shouldLog);
            CombatUiManager.ShowBoard(_firstEnemy, false);
        }

        currentPlayer.TurnIndicatorPreviewColor(true);
        playerToPreview.ShowPreviewIndicator(true);

        _previouslyShownPlayer = _previewedPlayer;
        _previewedPlayer = playerToPreview;

        // Disable UI elements during preview
        _uiManager.SetNextButtonState(false);
        _uiManager.SetAttackButtonState(false);

        switch (_previewedPlayer.IsAlly)
        {
            case true:
                Logger.Debug($"Start: Hiding current player: {currentPlayer.PlayerName}'s board", _shouldLog);
                CombatUiManager.ShowBoard(currentPlayer, false);
                CombatUiManager.ShowHand(currentPlayer, false);
                CombatUiManager.ShowHand(playerToPreview, true, true);
                break;
            case false:
                CombatUiManager.ShowHand(currentPlayer, true, true);
                if (_previouslyShownPlayer is { IsAlly: true })
                {
                    CombatUiManager.ShowHand(_previouslyShownPlayer, false);
                }

                if (playerToPreview != lastEnemy && lastEnemy != null)
                {
                    Logger.Debug($"Start: [2] Showing player to preview: {playerToPreview.PlayerName}'s board",
                        _shouldLog);
                    CombatUiManager.ShowBoard(playerToPreview, true);
                    Logger.Debug($"Start: Hiding last enemy: {lastEnemy.PlayerName}'s board", _shouldLog);
                    CombatUiManager.ShowBoard(lastEnemy, false);
                }

                break;
        }
    }

    private void EndPreview(Player currentPlayer, Player playerToPreview)
    {
        if (_previewedPlayer == null)
        {
            return;
        }

        Logger.Debug($"Ending Preview for {_previewedPlayer.PlayerName}", _shouldLog);
        Player lastEnemy = _uiManager.LastEnemy;

        _previewedPlayer.ShowPreviewIndicator(false);
        CombatUiManager.ShowBoard(currentPlayer, true);

        if (lastEnemy != null)
        {
            Logger.Debug($"End: [1] Showing last enemy: {lastEnemy.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(lastEnemy, true);
        }

        if (_previewedPlayer.IsAlly && _previewedPlayer != currentPlayer)
        {
            CombatUiManager.ShowHand(_previewedPlayer, false);
        }

        // Re-enable UI elements
        _uiManager.SetNextButtonState(true);
        if (!_combatManager.HasAttacked)
        {
            _uiManager.SetAttackButtonState(true);
        }

        if (currentPlayer.IsAlly)
        {
            if (currentPlayer.CurrentCardPlays >= currentPlayer.TotalCardPlays)
            {
                Logger.Debug($"Current Player Card Plays: {currentPlayer.CurrentCardPlays}", _shouldLog);
                CombatUiManager.ShowHand(currentPlayer, true, true);
            }
            else
            {
                CombatUiManager.ShowHand(currentPlayer, true);
            }
        }

        Logger.Debug($"End: Hiding previewed player: {_previewedPlayer.PlayerName}'s board", _shouldLog);
        CombatUiManager.ShowBoard(_previewedPlayer, false);

        if (!_previewedPlayer.IsAlly && playerToPreview.IsAlly)
        {
            Logger.Debug(
                $"End: [2] Showing {(lastEnemy != null ? "last enemy" : "first enemy")}: {(lastEnemy != null ? lastEnemy.PlayerName : _firstEnemy.PlayerName)}'s board",
                _shouldLog
            );
            CombatUiManager.ShowBoard(lastEnemy ?? _firstEnemy, true);
        }
        else if (_previewedPlayer.IsAlly && !playerToPreview.IsAlly)
        {
            Logger.Debug($"End: Hiding first enemy: {_firstEnemy.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(_firstEnemy, false);
        }

        currentPlayer.TurnIndicatorPreviewColor(false);
        _previewedPlayer = null;
        _previouslyShownPlayer = null;

        if (currentPlayer == playerToPreview)
        {
            Exit();
        }
    }
}
