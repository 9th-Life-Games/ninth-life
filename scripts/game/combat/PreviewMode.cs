using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class PreviewMode : ICombatMode
{
    private readonly CombatManager _combatManager;
    private readonly Player _firstEnemy;
    private readonly bool _shouldLog = true;
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

    public void Enter(bool isForward = true)
    {
        _previewIndex = _turnManager.GetCurrentPlayerIndex();
        _uiManager.SetNextButtonState(false);
        _uiManager.SetAttackButtonState(false);
        _turnManager.CurrentPlayer.TurnIndicatorPreviewColor(true);

        // Show initial preview based on direction
        if (isForward)
        {
            _previewIndex = _turnManager.GetCurrentPlayerIndex() + 1;
            ShowPreview(_turnManager.GetNextPlayer());
        }
        else
        {
            _previewIndex = _turnManager.GetTurnOrderCount() - 1;
            ShowPreview(_turnManager.GetPlayerToPreview(_previewIndex));
        }
    }

    public void Exit()
    {
        EndPreview(_turnManager.CurrentPlayer);
    }

    public void Update(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed("previous_preview"))
        {
            CyclePreviewBackward();
        }
        else if (inputEvent.IsActionPressed("next_preview"))
        {
            CyclePreviewForward();
        }
    }

    private void CyclePreviewForward()
    {
        if (!_turnManager.CurrentPlayer.IsAlly)
        {
            return;
        }

        Logger.Debug($"***Current preview index {_previewIndex}");
        if (_previewIndex == -1)
        {
            Logger.Debug("***Preview -1: setting index back to start");
            _previewIndex = _turnManager.GetCurrentPlayerIndex();
        }

        _previewIndex++;
        if (_previewIndex > _turnManager.GetTurnOrderCount() - 1)
        {
            Logger.Debug("***Preview > player count: setting index back to start");
            _previewIndex = _turnManager.GetCurrentPlayerIndex();
        }

        Logger.Debug($"***Next preview index: {_previewIndex}");
        ShowPreview(_turnManager.GetPlayerToPreview(_previewIndex));
    }

    private void CyclePreviewBackward()
    {
        if (!_turnManager.CurrentPlayer.IsAlly)
        {
            return;
        }

        if (_previewIndex == -1)
        {
            _previewIndex = _turnManager.GetCurrentPlayerIndex();
        }

        _previewIndex--;
        if (_previewIndex < 0)
        {
            _previewIndex = _turnManager.GetTurnOrderCount() - 1;
        }

        Logger.Debug($"***Previous preview index: {_previewIndex}");
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
            EndPreview(currentPlayer, true);
            return;
        }

        EndPreview(currentPlayer, playerToPreview.IsAlly);
        Logger.Debug($"Start: [1] Showing player to preview: {playerToPreview.PlayerName}'s board", _shouldLog);

        CombatUiManager.ShowBoard(playerToPreview, true);
        if (_previouslyShownPlayer == null && lastEnemy == null && !playerToPreview.IsAlly &&
            playerToPreview != _firstEnemy)
        {
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

    private void EndPreview(Player currentPlayer, bool isAllyNext = false)
    {
        if (_previewedPlayer == null)
        {
            return;
        }

        Logger.Debug("Ending Preview", _shouldLog);
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
                Logger.Debug($"Current Player Card Plays: {currentPlayer.CurrentCardPlays}");
                CombatUiManager.ShowHand(currentPlayer, true, true);
            }
            else
            {
                CombatUiManager.ShowHand(currentPlayer, true);
            }
        }

        if (_previewedPlayer != null)
        {
            Logger.Debug($"End: Hiding previewed player: {_previewedPlayer.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(_previewedPlayer, false);
        }

        if (!_previewedPlayer.IsAlly && isAllyNext)
        {
            Logger.Debug(
                $"End: [2] Showing {(lastEnemy != null ? "last enemy" : "first enemy")}: {(lastEnemy != null ? lastEnemy.PlayerName : _firstEnemy.PlayerName)}'s board",
                _shouldLog
            );
            CombatUiManager.ShowBoard(lastEnemy ?? _firstEnemy, true);
        }
        else if (_previewedPlayer.IsAlly && !isAllyNext)
        {
            Logger.Debug($"End: Hiding first enemy: {_firstEnemy.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(_firstEnemy, false);
        }

        currentPlayer.TurnIndicatorPreviewColor(false);
        _previewedPlayer = null;
        _previouslyShownPlayer = null;
    }
}
