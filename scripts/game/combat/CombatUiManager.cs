using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class CombatUiManager
{
    private static readonly bool ShouldLog = true;
    private Button _attackButton;
    private Button _endTurnButton;

    private Player LastAlly { get; set; }
    public Player LastEnemy { get; private set; }

    public void SetButtons(Button endTurnButton, Button attackButton)
    {
        Logger.Debug("Setting up UI buttons", ShouldLog);
        _endTurnButton = endTurnButton;
        _attackButton = attackButton;
    }

    public static void ShowBoard(Player player, bool show)
    {
        Logger.Debug($"{(show ? "Showing" : "Hiding")} board for {player.PlayerName}", ShouldLog);
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
            Logger.Debug($"Hiding hand for {player.PlayerName}", ShouldLog);
            player.SlideHandOut();
            return;
        }

        if (disabled)
        {
            if (!player.IsAlly)
            {
                Logger.Debug($"Warning: Attempting to disable hand for non-ally {player.PlayerName}", ShouldLog);
            }

            Logger.Debug($"Showing disabled hand for {player.PlayerName}", ShouldLog);
            player.SlideHandDisabled();
        }
        else
        {
            Logger.Debug($"Showing enabled hand for {player.PlayerName}", ShouldLog);
            player.SlideHandIn();
            player.EnablePlayerHand();
        }
    }

    public void SetNextButtonState(bool enabled)
    {
        Logger.Debug($"Setting next turn button enabled: {enabled}", ShouldLog);
        _endTurnButton.Disabled = !enabled;
    }

    public void SetAttackButtonState(bool enabled)
    {
        Logger.Debug($"Setting attack button enabled: {enabled}", ShouldLog);
        _attackButton.Disabled = !enabled;
    }

    private void TrackLastPlayer(Player player)
    {
        if (player.IsAlly)
        {
            Logger.Debug($"Tracking last ally: {player.PlayerName}", ShouldLog);
            LastAlly = player;
        }
        else
        {
            Logger.Debug($"Tracking last enemy: {player.PlayerName}", ShouldLog);
            LastEnemy = player;
        }
    }

    public void HideLastPlayerBoard(bool nextPlayerIsAlly)
    {
        Player lastPlayer = nextPlayerIsAlly ? LastAlly : LastEnemy;
        if (lastPlayer != null)
        {
            Logger.Debug($"Hiding last player board: {lastPlayer.PlayerName}", ShouldLog);
            ShowBoard(lastPlayer, false);
        }
    }

    public static void HideLastAllyHand(Player lastAlly)
    {
        if (lastAlly != null)
        {
            Logger.Debug($"Hiding last ally hand: {lastAlly.PlayerName}", ShouldLog);
            ShowHand(lastAlly, false);
        }
    }

    public void HandleCurrentAllyHandExit(Player currentPlayer, Player nextPlayer)
    {
        Logger.Debug($"Handling hand exit for {currentPlayer.PlayerName}", ShouldLog);
        TrackLastPlayer(currentPlayer);

        bool shouldShowDisabled = currentPlayer.IsAlly && !nextPlayer.IsAlly;
        HandleHandVisibility(currentPlayer, shouldShowDisabled);
    }

    private void HandleHandVisibility(Player player, bool showDisabled)
    {
        if (showDisabled)
        {
            Logger.Debug($"Showing disabled hand for transitioning ally {player.PlayerName}", ShouldLog);
            ShowHand(player, true, true);
        }
        else
        {
            Logger.Debug($"Hiding hand for {player.PlayerName}", ShouldLog);
            ShowHand(player, false);
        }
    }

    public void InitPlayerUi(Player currentPlayer, Player firstAlly, Player firstEnemy)
    {
        Logger.Debug("Initializing player UI", ShouldLog);
        TrackLastPlayer(currentPlayer);

        InitializeHands(currentPlayer, firstAlly);
        InitializeBoards(firstAlly, firstEnemy);
    }

    private void InitializeHands(Player currentPlayer, Player firstAlly)
    {
        Player handPlayer = currentPlayer.IsAlly ? currentPlayer : firstAlly;
        Logger.Debug($"Setting up initial hand for {handPlayer.PlayerName}", ShouldLog);
        ShowHand(handPlayer, true, !currentPlayer.IsAlly);
    }

    private void InitializeBoards(Player firstAlly, Player firstEnemy)
    {
        Logger.Debug($"Showing initial boards for {firstAlly.PlayerName} and {firstEnemy.PlayerName}", ShouldLog);
        ShowBoard(firstAlly, true);
        ShowBoard(firstEnemy, true);
    }

    public void ShowPlayerUi(Player player)
    {
        Logger.Debug($"Showing UI for player: {player.PlayerName}", ShouldLog);
        HandleHandTransition(player);
        ShowBoard(player, true);
    }

    private void HandleHandTransition(Player player)
    {
        if (!player.IsAlly)
        {
            return;
        }

        if (LastAlly != null)
        {
            Logger.Debug($"Hiding previous ally hand: {LastAlly.PlayerName}", ShouldLog);
            ShowHand(LastAlly, false);
        }

        Logger.Debug($"Showing new ally hand: {player.PlayerName}", ShouldLog);
        ShowHand(player, true);
    }

    public void SetLastEnemy(Player enemy)
    {
        if (!enemy.IsAlly)
        {
            Logger.Debug($"Setting last enemy to: {enemy.PlayerName}", ShouldLog);
            LastEnemy = enemy;
        }
        else
        {
            Logger.Debug($"Attempted to set ally {enemy.PlayerName} as last enemy", ShouldLog);
        }
    }
}
