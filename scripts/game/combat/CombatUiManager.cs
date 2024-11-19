using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class CombatUiManager
{
    private static readonly bool ShouldLog = true;
    private Button _attackButton;
    private Button _endTurnButton;

    public Player LastAlly { get; private set; }
    public Player LastEnemy { get; private set; }

    public void SetButtons(Button endTurnButton, Button attackButton)
    {
        Logger.Debug("CombatUiManager: Setting up UI buttons", ShouldLog);
        _endTurnButton = endTurnButton;
        _attackButton = attackButton;
        _endTurnButton.Disabled = true;
        _attackButton.Disabled = true;
    }

    public static void ShowBoard(Player player, bool show)
    {
        Logger.Debug($"CombatUiManager: {(show ? "Showing" : "Hiding")} board for {player.PlayerName}", ShouldLog);
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
            Logger.Debug($"CombatUiManager: Hiding hand for {player.PlayerName}", ShouldLog);
            player.SlideHandOut();
            return;
        }

        if (disabled)
        {
            if (!player.IsAlly)
            {
                Logger.Debug($"CombatUiManager: Warning: Attempting to disable hand for non-ally {player.PlayerName}",
                    ShouldLog);
            }

            Logger.Debug($"CombatUiManager: Showing disabled hand for {player.PlayerName}", ShouldLog);
            player.SlideHandDisabled();
        }
        else
        {
            Logger.Debug($"CombatUiManager: Showing enabled hand for {player.PlayerName}", ShouldLog);
            player.SlideHandIn();
            player.EnablePlayerHand();
        }
    }

    public void SetEndTurnButtonState(bool enabled)
    {
        Logger.Debug($"CombatUiManager: Setting end turn button enabled: {enabled}", ShouldLog);
        _endTurnButton.Disabled = !enabled;
    }

    public void SetAttackButtonState(bool enabled)
    {
        Logger.Debug($"CombatUiManager: Setting attack button enabled: {enabled}", ShouldLog);
        _attackButton.Disabled = !enabled;
    }

    private void TrackLastPlayer(Player player)
    {
        if (player.IsAlly)
        {
            Logger.Debug($"CombatUiManager: Tracking last ally: {player.PlayerName}", ShouldLog);
            LastAlly = player;
        }
        else
        {
            Logger.Debug($"CombatUiManager: Tracking last enemy: {player.PlayerName}", ShouldLog);
            LastEnemy = player;
        }
    }

    public void HideLastPlayerBoard(bool nextPlayerIsAlly)
    {
        Player lastPlayer = nextPlayerIsAlly ? LastAlly : LastEnemy;
        if (lastPlayer != null)
        {
            Logger.Debug($"CombatUiManager: Hiding last player board: {lastPlayer.PlayerName}", ShouldLog);
            ShowBoard(lastPlayer, false);
        }
    }

    public static void HideLastAllyHand(Player lastAlly)
    {
        if (lastAlly != null)
        {
            Logger.Debug($"CombatUiManager: Hiding last ally hand: {lastAlly.PlayerName}", ShouldLog);
            ShowHand(lastAlly, false);
        }
    }

    public void HandleCurrentAllyHandExit(Player currentPlayer, Player nextPlayer)
    {
        Logger.Debug($"CombatUiManager: Handling hand exit for {currentPlayer.PlayerName}", ShouldLog);
        TrackLastPlayer(currentPlayer);

        bool shouldShowDisabled = currentPlayer.IsAlly && !nextPlayer.IsAlly;
        HandleHandVisibility(currentPlayer, shouldShowDisabled);
    }

    private void HandleHandVisibility(Player player, bool showDisabled)
    {
        if (showDisabled)
        {
            Logger.Debug($"CombatUiManager: Showing disabled hand for transitioning ally {player.PlayerName}",
                ShouldLog);
            ShowHand(player, true, true);
        }
        else
        {
            Logger.Debug($"CombatUiManager: Hiding hand for {player.PlayerName}", ShouldLog);
            ShowHand(player, false);
        }
    }

    public void InitPlayerUi(Player currentPlayer, Player firstAlly, Player firstEnemy)
    {
        Logger.Debug("CombatUiManager: Initializing player UI", ShouldLog);
        TrackLastPlayer(currentPlayer);

        InitializeHands(currentPlayer, firstAlly);
        InitializeBoards(firstAlly, firstEnemy);
    }

    private void InitializeHands(Player currentPlayer, Player firstAlly)
    {
        Player handPlayer = currentPlayer.IsAlly ? currentPlayer : firstAlly;
        Logger.Debug($"CombatUiManager: Setting up initial hand for {handPlayer.PlayerName}", ShouldLog);
        ShowHand(handPlayer, true, !currentPlayer.IsAlly);
    }

    private void InitializeBoards(Player firstAlly, Player firstEnemy)
    {
        Logger.Debug($"CombatUiManager: Showing initial boards for {firstAlly.PlayerName} and {firstEnemy.PlayerName}",
            ShouldLog);
        ShowBoard(firstAlly, true);
        ShowBoard(firstEnemy, true);
    }

    public void ShowPlayerUi(Player player)
    {
        Logger.Debug($"CombatUiManager: Showing UI for player: {player.PlayerName}", ShouldLog);
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
            Logger.Debug($"CombatUiManager: Hiding previous ally hand: {LastAlly.PlayerName}", ShouldLog);
            ShowHand(LastAlly, false);
        }

        Logger.Debug($"CombatUiManager: Showing new ally hand: {player.PlayerName}", ShouldLog);
        ShowHand(player, true);
    }

    public void SetLastEnemy(Player enemy)
    {
        if (!enemy.IsAlly)
        {
            Logger.Debug($"CombatUiManager: Setting last enemy to: {enemy.PlayerName}", ShouldLog);
            LastEnemy = enemy;
        }
        else
        {
            Logger.Debug($"CombatUiManager: Attempted to set ally {enemy.PlayerName} as last enemy", ShouldLog);
        }
    }

    public void HandlePlayerDeath(Player target, Player replacementPlayer)
    {
        ShowHand(target, false);
        ShowBoard(target, false);
        if (target.IsAlly)
        {
            LastAlly = replacementPlayer;
            ShowHand(LastAlly, true, true);
            ShowBoard(LastAlly, true);
        }
        else
        {
            LastEnemy = replacementPlayer;
            ShowBoard(LastEnemy, true);
        }
    }
}
