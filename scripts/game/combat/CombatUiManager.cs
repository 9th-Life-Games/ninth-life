using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class CombatUiManager
{
    private Button _attackButton;
    private Button _endTurnButton;
    private Player LastAlly { get; set; }
    public Player LastEnemy { get; private set; }

    public void SetButtons(Button endTurnButton, Button attackButton)
    {
        _endTurnButton = endTurnButton;
        _attackButton = attackButton;
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
            if (!player.IsAlly)
            {
                Logger.Debug("****Ooooops****");
            }

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
        Logger.Debug($"SetNextButtonState: {enabled}");
        _endTurnButton.Disabled = !enabled;
    }

    public void SetAttackButtonState(bool enabled)
    {
        Logger.Debug($"SetAttackButtonState: {enabled}");
        _attackButton.Disabled = !enabled;
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
    }

    public void ShowPlayerUi(Player player)
    {
        if (player.IsAlly)
        {
            if (LastAlly != null)
            {
                ShowHand(LastAlly, false);
            }

            ShowHand(player, true);
        }

        ShowBoard(player, true);
    }

    public void SetLastEnemy(Player enemy)
    {
        if (!enemy.IsAlly)
        {
            LastEnemy = enemy;
        }
    }
}
