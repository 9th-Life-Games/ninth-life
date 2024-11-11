using Godot;
using NinthLife.scripts.game;
using NinthLife.scripts.game.combat;

namespace NinthLife.scripts.utils;

public class CombatUiManager
{
    private Button _attackButton;
    private Button _endTurnButton;
    private bool _isInPreviewMode;
    private Player _previewedPlayer;
    private Player _previouslyShownPlayer;
    private TurnManager _turnManager;

    public Player LastAlly { get; private set; }
    public Player LastEnemy { get; private set; }

    #region Button Management

    public void InitUiManager(Button endTurnButton, Button attackButton, TurnManager turnManager)
    {
        _endTurnButton = endTurnButton;
        _attackButton = attackButton;
        _turnManager = turnManager;
    }

    public void SetEndTurnButtonState(bool enabled)
    {
        _endTurnButton.Disabled = !enabled;
    }

    public void SetAttackButtonState(bool enabled)
    {
        _attackButton.Disabled = !enabled;
    }

    #endregion

    #region Board and Hand Management

    public static void ShowBoard(Player player, bool show)
    {
        if (show)
        {
            player.SlideBoardIn();
        }
        else
        {
            player.SlideBoardOut();
        }
    }

    public static void ShowHand(Player player, bool show, bool disabled = false)
    {
        if (!show)
        {
            player.SlideHandOut();
            return;
        }

        if (disabled)
        {
            player.SlideHandDisabled();
        }
        else
        {
            player.SlideHandIn();
            player.EnablePlayerHand();
        }
    }

    #endregion

    #region Player Tracking

    private void TrackLastPlayer(Player player)
    {
        if (player == null)
        {
            return;
        }

        if (player.IsAlly)
        {
            LastAlly = player;
        }
        else
        {
            LastEnemy = player;
        }
    }

    public void HideLastPlayerBoard(bool nextPlayerIsAlly)
    {
        Player lastPlayer = nextPlayerIsAlly ? LastAlly : LastEnemy;
        if (lastPlayer != null)
        {
            ShowBoard(lastPlayer, false);
        }
    }

    public static void HideLastAllyHand(Player lastAlly)
    {
        if (lastAlly != null)
        {
            ShowHand(lastAlly, false);
        }
    }

    #endregion

    #region Preview Management

    public void StartPreview(Player playerToPreview, Player currentPlayer)
    {
        // If previewing current player, end preview instead
        if (playerToPreview == currentPlayer)
        {
            EndPreview(currentPlayer, true);
            return;
        }

        EndPreview(currentPlayer, playerToPreview.IsAlly);
        ShowBoard(playerToPreview, true);

        if (_previouslyShownPlayer == null && LastEnemy == null &&
            !playerToPreview.IsAlly && playerToPreview != _turnManager.FirstEnemy)
        {
            ShowBoard(_turnManager.FirstEnemy, false);
        }

        _isInPreviewMode = true;
        currentPlayer.TurnIndicatorPreviewColor(true);
        playerToPreview.ShowPreviewIndicator(true);

        _previouslyShownPlayer = _previewedPlayer;
        _previewedPlayer = playerToPreview;

        // Disable UI elements during preview
        SetEndTurnButtonState(false);
        SetAttackButtonState(false);

        if (_previewedPlayer.IsAlly)
        {
            HandleAllyPreview(currentPlayer, playerToPreview);
        }
        else
        {
            HandleEnemyPreview(currentPlayer, playerToPreview);
        }
    }

    private void HandleAllyPreview(Player currentPlayer, Player playerToPreview)
    {
        ShowBoard(currentPlayer, false);
        ShowHand(currentPlayer, false);
        ShowHand(playerToPreview, true, true);
    }

    private void HandleEnemyPreview(Player currentPlayer, Player playerToPreview)
    {
        ShowHand(currentPlayer, true, true);

        if (_previouslyShownPlayer?.IsAlly == true)
        {
            ShowHand(_previouslyShownPlayer, false);
        }

        if (playerToPreview != LastEnemy && LastEnemy != null)
        {
            ShowBoard(playerToPreview, true);
            ShowBoard(LastEnemy, false);
        }
    }

    public void EndPreview(Player currentPlayer, bool isAllyNext = false)
    {
        if (!_isInPreviewMode)
        {
            return;
        }

        Player firstEnemy = _turnManager.FirstEnemy;

        _previewedPlayer.ShowPreviewIndicator(false);
        ShowBoard(currentPlayer, true);

        if (LastEnemy != null)
        {
            ShowBoard(LastEnemy, true);
        }

        if (_previewedPlayer.IsAlly && _previewedPlayer != currentPlayer)
        {
            ShowHand(_previewedPlayer, false);
        }

        // Re-enable UI elements
        SetEndTurnButtonState(true);
        if (currentPlayer.HasAttacked)
        {
            SetAttackButtonState(true);
        }

        HandleCurrentPlayerHand(currentPlayer);
        HandleBoardTransitions(currentPlayer, isAllyNext);

        _isInPreviewMode = false;
        currentPlayer.TurnIndicatorPreviewColor(false);
        _previewedPlayer = null;
        _previouslyShownPlayer = null;
    }

    private void HandleCurrentPlayerHand(Player currentPlayer)
    {
        if (currentPlayer.IsAlly)
        {
            ShowHand(currentPlayer, true, currentPlayer.CurrentCardPlays >= currentPlayer.TotalCardPlays);
        }
    }

    private void HandleBoardTransitions(Player currentPlayer, bool isAllyNext)
    {
        Player firstEnemy = _turnManager.FirstEnemy;
        if (_previewedPlayer != null)
        {
            ShowBoard(_previewedPlayer, false);
        }

        if (!_previewedPlayer.IsAlly && isAllyNext)
        {
            ShowBoard(LastEnemy ?? firstEnemy, true);
        }
        else if (_previewedPlayer.IsAlly && !isAllyNext)
        {
            ShowBoard(firstEnemy, false);
        }
    }

    #endregion

    #region Turn Management

    public void HandleCurrentAllyHandExit(Player currentPlayer, Player nextPlayer)
    {
        TrackLastPlayer(currentPlayer);

        if (currentPlayer.IsAlly && !nextPlayer.IsAlly)
        {
            ShowHand(currentPlayer, true, true);
        }
        else
        {
            ShowHand(currentPlayer, false);
        }
    }

    public void InitPlayerUi(Player currentPlayer, Player firstAlly, Player firstEnemy)
    {
        TrackLastPlayer(currentPlayer);
        ShowHand(currentPlayer.IsAlly ? currentPlayer : firstAlly, true, !currentPlayer.IsAlly);
        ShowBoard(firstAlly, true);
        ShowBoard(firstEnemy, true);
        if (currentPlayer.IsAlly)
        {
            currentPlayer.AllyHandEnabled += SetEndTurnButtonState;
        }
    }

    public void ShowPlayerUi(Player player)
    {
        if (player.IsAlly)
        {
            ShowHand(player, true);
        }

        ShowBoard(player, true);
        if (player.IsAlly)
        {
            player.AllyHandEnabled += SetEndTurnButtonState;
        }
    }

    #endregion
}
