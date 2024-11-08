using Godot;
using NinthLife.scripts.game;

namespace NinthLife.scripts.utils;

public class CombatUiManager
{
    private Button _endTurnButton;
    public Player LastAlly { get; private set; }
    public Player LastEnemy { get; private set; }

    public void SetEndTurnButton(Button endTurnButton)
    {
        _endTurnButton = endTurnButton;
    }

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

    public void SetNextButtonState(bool enabled)
    {
        Logger.Debug("SetNextButtonState");
        _endTurnButton.Disabled = !enabled;
    }

    private void TrackLastPlayer(Player player)
    {
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
            currentPlayer.AllyHandEnabled += SetNextButtonState;
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
            player.AllyHandEnabled += SetNextButtonState;
        }
    }
}
