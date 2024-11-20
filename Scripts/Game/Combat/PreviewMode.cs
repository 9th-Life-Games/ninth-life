using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class PreviewMode : ICombatMode
{
    public delegate void PreviewEndedEventHandler();

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

    public void Enter(bool isForward = true, Player playerToPreview = null)
    {
        Logger.Debug("PreviewMode: Entering Preview Mode", _shouldLog);
        InitializePreviewMode();

        if (playerToPreview != null)
        {
            _previewIndex = _turnManager.GetPlayerIndex(playerToPreview);
            ShowPreview(playerToPreview);
            return;
        }

        InitializePreviewIndex(isForward);
        ShowPreview(_turnManager.GetPlayerToPreview(_previewIndex));
    }

    public void Exit()
    {
        Logger.Debug("PreviewMode: Removing preview click listeners", _shouldLog);
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
            Logger.Debug("PreviewMode: CyclePreviewForward called", _shouldLog);
            CyclePreviewForward();
        }
    }

    public event PreviewEndedEventHandler PreviewEnded;

    private void InitializePreviewMode()
    {
        _previewIndex = _turnManager.GetCurrentPlayerIndex();
        _uiManager.SetEndTurnButtonState(false);
        _uiManager.SetAttackButtonState(false);
        _turnManager.CurrentPlayer.TurnIndicatorPreviewColor(true);

        foreach (Player player in _turnManager.TurnOrder)
        {
            player.PlayerClicked += OnPlayerClicked;
        }
    }

    private void InitializePreviewIndex(bool isForward)
    {
        _previewIndex = isForward
            ? _turnManager.GetCurrentPlayerIndex() + 1
            : _turnManager.GetTurnOrderCount() - 1;

        if (_previewIndex >= _turnManager.GetTurnOrderCount())
        {
            _previewIndex = 0;
        }
    }

    private void OnPlayerClicked(Player player)
    {
        _previewIndex = _turnManager.GetPlayerIndex(player);
        ShowPreview(player);
    }

    private void CyclePreviewForward()
    {
        Logger.Debug($"PreviewMode: Current preview index: {_previewIndex}", _shouldLog);

        if (_previewIndex == -1)
        {
            Logger.Debug("PreviewMode: Preview index -1: setting to start", _shouldLog);
            _previewIndex = _turnManager.GetCurrentPlayerIndex();
        }

        _previewIndex++;
        if (_previewIndex > _turnManager.GetTurnOrderCount() - 1)
        {
            Logger.Debug("PreviewMode: Preview index exceeded count: resetting to start", _shouldLog);
            _previewIndex = _turnManager.GetCurrentPlayerIndex();
        }

        Logger.Debug($"PreviewMode: Next preview index: {_previewIndex}", _shouldLog);
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

        Logger.Debug($"PreviewMode: Previous preview index: {_previewIndex}", _shouldLog);
        ShowPreview(_turnManager.GetPlayerToPreview(_previewIndex));
    }

    private void ShowPreview(Player playerToPreview)
    {
        Player currentPlayer = _turnManager.CurrentPlayer;
        Player lastEnemy = _uiManager.LastEnemy;
        Logger.Debug($"PreviewMode: Starting Preview for: {playerToPreview.PlayerName}", _shouldLog);

        if (playerToPreview == currentPlayer)
        {
            Logger.Debug("PreviewMode: Preview is current player, ending preview", _shouldLog);
            EndPreview(currentPlayer, playerToPreview);
            return;
        }

        EndPreview(currentPlayer, playerToPreview);
        HandleBoardTransitions(playerToPreview, currentPlayer, lastEnemy);
        UpdatePreviewState(playerToPreview, currentPlayer);
    }

    private void HandleBoardTransitions(Player playerToPreview, Player currentPlayer, Player lastEnemy)
    {
        Logger.Debug($"PreviewMode: Showing board for: {playerToPreview.PlayerName}", _shouldLog);
        CombatUiManager.ShowBoard(playerToPreview, true);

        if (ShouldHideFirstEnemy(playerToPreview, lastEnemy))
        {
            Logger.Debug("PreviewMode: Hiding first enemy board", _shouldLog);
            CombatUiManager.ShowBoard(_firstEnemy, false);
        }

        if (playerToPreview.IsAlly)
        {
            HandleAllyPreview(playerToPreview, currentPlayer);
        }
        else
        {
            HandleEnemyPreview(playerToPreview, currentPlayer, lastEnemy);
        }
    }

    private bool ShouldHideFirstEnemy(Player playerToPreview, Player lastEnemy)
    {
        return _previouslyShownPlayer == null && lastEnemy == null && !playerToPreview.IsAlly &&
               playerToPreview != _firstEnemy;
    }

    private void HandleAllyPreview(Player playerToPreview, Player currentPlayer)
    {
        Logger.Debug($"PreviewMode: Hiding current player: {currentPlayer.PlayerName}'s board", _shouldLog);
        CombatUiManager.ShowBoard(currentPlayer, false);
        CombatUiManager.ShowHand(currentPlayer, false);
        CombatUiManager.ShowHand(playerToPreview, true, true);
    }

    private void HandleEnemyPreview(Player playerToPreview, Player currentPlayer, Player lastEnemy)
    {
        CombatUiManager.ShowHand(currentPlayer, true, true);

        if (_previouslyShownPlayer is { IsAlly: true })
        {
            CombatUiManager.ShowHand(_previouslyShownPlayer, false);
        }

        if (playerToPreview != lastEnemy && lastEnemy != null)
        {
            Logger.Debug(
                $"PreviewMode: Transitioning enemy boards from {lastEnemy.PlayerName} to {playerToPreview.PlayerName}",
                _shouldLog);
            CombatUiManager.ShowBoard(playerToPreview, true);
            CombatUiManager.ShowBoard(lastEnemy, false);
        }
    }

    private void UpdatePreviewState(Player playerToPreview, Player currentPlayer)
    {
        currentPlayer.TurnIndicatorPreviewColor(true);
        playerToPreview.ShowPreviewIndicator(true);

        _previouslyShownPlayer = _previewedPlayer;
        _previewedPlayer = playerToPreview;

        _uiManager.SetEndTurnButtonState(false);
        _uiManager.SetAttackButtonState(false);
    }

    private void EndPreview(Player currentPlayer, Player playerToPreview)
    {
        if (_previewedPlayer == null)
        {
            return;
        }

        Logger.Debug($"PreviewMode: Ending Preview for {_previewedPlayer.PlayerName}", _shouldLog);
        HandlePreviewEnd(currentPlayer, playerToPreview);
        ResetPreviewState(currentPlayer, playerToPreview);
    }

    private void HandlePreviewEnd(Player currentPlayer, Player playerToPreview)
    {
        Player lastEnemy = _uiManager.LastEnemy;

        _previewedPlayer.ShowPreviewIndicator(false);
        CombatUiManager.ShowBoard(currentPlayer, true);

        if (lastEnemy != null)
        {
            Logger.Debug($"PreviewMode: Showing last enemy: {lastEnemy.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(lastEnemy, true);
        }

        if (_previewedPlayer.IsAlly && _previewedPlayer != currentPlayer)
        {
            CombatUiManager.ShowHand(_previewedPlayer, false);
        }

        RestoreUiState(currentPlayer);
        HandleBoardVisibility(playerToPreview, lastEnemy);
    }

    private void RestoreUiState(Player currentPlayer)
    {
        _uiManager.SetEndTurnButtonState(true);
        if (_combatManager.TotalAttacksThisTurn >= 1)
        {
            _uiManager.SetAttackButtonState(true);
        }

        if (currentPlayer.IsAlly)
        {
            bool handDisabled = currentPlayer.CurrentCardPlays >= currentPlayer.TotalCardPlays;
            CombatUiManager.ShowHand(currentPlayer, true, handDisabled);
        }
    }

    private void HandleBoardVisibility(Player playerToPreview, Player lastEnemy)
    {
        Logger.Debug($"PreviewMode: Hiding previewed player: {_previewedPlayer.PlayerName}'s board", _shouldLog);
        CombatUiManager.ShowBoard(_previewedPlayer, false);

        if (!_previewedPlayer.IsAlly && playerToPreview.IsAlly)
        {
            Player boardToShow = lastEnemy ?? _firstEnemy;
            Logger.Debug($"PreviewMode: Showing {boardToShow.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(boardToShow, true);
        }
        else if (_previewedPlayer.IsAlly && !playerToPreview.IsAlly)
        {
            Logger.Debug($"PreviewMode: Hiding first enemy: {_firstEnemy.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(_firstEnemy, false);
        }
    }

    private void ResetPreviewState(Player currentPlayer, Player playerToPreview)
    {
        currentPlayer.TurnIndicatorPreviewColor(false);
        _previewedPlayer = null;
        _previouslyShownPlayer = null;

        if (currentPlayer == playerToPreview)
        {
            Logger.Debug("PreviewMode: Preview ended on current player", _shouldLog);
            Exit();
            PreviewEnded?.Invoke();
        }
    }
}
