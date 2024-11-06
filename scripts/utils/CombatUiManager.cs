using Godot;
using NinthLife.scripts.game;

namespace NinthLife.scripts.utils;

public class CombatUiManager
{
    private Player _lastAlly;
    private Player _lastEnemy;
    private Button _nextButton;

    public void SetNextButton(Button nextButton)
    {
        _nextButton = nextButton;
    }

    private void ShowBoard(Player player, bool show)
    {
        SetNextButtonState(!player.IsAlly);
        if (show)
        {
            player.SlideBoardIn();
        }
        else
        {
            player.SlideBoardOut();
        }
    }

    private static void ShowHand(Player player, bool show, bool disabled = false)
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

    private void SetNextButtonState(bool enabled)
    {
        _nextButton.Disabled = enabled;
    }

    public void TrackLastPlayer(Player player)
    {
        if (player.IsAlly)
        {
            _lastAlly = player;
        }
        else
        {
            _lastEnemy = player;
        }
    }

    public void HideLastPlayerBoard(bool nextPlayerIsAlly)
    {
        Player lastPlayer = nextPlayerIsAlly ? _lastAlly : _lastEnemy;
        if (lastPlayer != null)
        {
            ShowBoard(lastPlayer, false);
        }
    }

    public void HideLastAllyHand(Player lastAlly)
    {
        if (lastAlly != null)
        {
            ShowHand(lastAlly, false);
        }
    }

    public void HandleCurrentAllyHandExit(Player currentPlayer, Player nextPlayer)
    {
        TrackLastPlayer(currentPlayer);

        if (!currentPlayer.IsAlly && !nextPlayer.IsAlly)
        {
            return;
        }

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
    }

    public void ShowPlayerUi(Player player)
    {
        if (player.IsAlly)
        {
            ShowHand(player, true);
        }

        ShowBoard(player, true);
    }
}
