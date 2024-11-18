using System.Collections.Generic;
using System.Linq;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class CombatTurnManager
{
    private readonly bool _shouldLog = true;

    public readonly List<Player> AllyTurnOrder = new();
    public readonly List<Player> EnemyTurnOrder = new();
    public readonly List<Player> TurnOrder = new();
    private TurnOrderDisplay _turnOrderDisplay;
    public Player CurrentPlayer { get; private set; }

    public void InitializePlayers(CombatManager combatManager, TurnOrderDisplay turnOrderDisplay)
    {
        Logger.Debug("CombatTurnManager: Initializing combat turn manager", _shouldLog);
        _turnOrderDisplay = turnOrderDisplay;

        List<Player> orderedPlayers = GameUtils.CombatEntities
            .OrderByDescending(static player => player.Initiative)
            .ToList();

        Logger.Debug("CombatTurnManager: Setting up turn order with players:", _shouldLog);
        foreach (Player player in orderedPlayers)
        {
            ProcessPlayer(player, combatManager);
        }

        InitializeFirstTurn();
    }

    public void RemovePlayer(Player player)
    {
        _turnOrderDisplay.RemoveAvatar(player);
    }

    private void ProcessPlayer(Player player, CombatManager combatManager)
    {
        Logger.Debug($"CombatTurnManager: Processing player: {player.PlayerName} (Initiative: {player.Initiative})",
            _shouldLog);

        if (CurrentPlayer == null)
        {
            CurrentPlayer = player;
            Logger.Debug($"CombatTurnManager: Set initial current player: {player.PlayerName}", _shouldLog);
        }

        TurnOrder.Add(player);
        _turnOrderDisplay.AddAvatar(player);

        if (player.IsAlly)
        {
            Logger.Debug($"CombatTurnManager: Adding {player.PlayerName} to ally turn order", _shouldLog);
            AllyTurnOrder.Add(player);
        }
        else
        {
            Logger.Debug($"CombatTurnManager: Adding {player.PlayerName} to enemy turn order", _shouldLog);
            EnemyTurnOrder.Add(player);
        }

        combatManager.AddChild(player);
    }

    private void InitializeFirstTurn()
    {
        Logger.Debug($"CombatTurnManager: Initializing first turn for {CurrentPlayer.PlayerName}", _shouldLog);
        CurrentPlayer.TurnIndicator.Visible = true;
    }

    public Player GetNextPlayer()
    {
        Player nextPlayer = null;
        for (int i = 1; i < TurnOrder.Count; i++)
        {
            if (!TurnOrder[i].IsDead)
            {
                nextPlayer ??= TurnOrder[i];
            }
            // else if (nextPlayer == null)
            // {
            //     TurnOrder.Remove(CurrentPlayer);
            //     TurnOrder.Add(CurrentPlayer);
            // }
        }

        // Player nextPlayer = TurnOrder[1];
        Logger.Debug($"CombatTurnManager: Next player will be: {nextPlayer?.PlayerName}", _shouldLog);
        return nextPlayer;
    }

    public int GetCurrentPlayerIndex()
    {
        int index = TurnOrder.IndexOf(CurrentPlayer);
        Logger.Debug($"CombatTurnManager: Current player {CurrentPlayer.PlayerName} index: {index}", _shouldLog);
        return index;
    }

    public int GetPlayerIndex(Player player)
    {
        int index = TurnOrder.IndexOf(player);
        Logger.Debug($"CombatTurnManager: Player {player.PlayerName} index: {index}", _shouldLog);
        return index;
    }

    public int GetTurnOrderCount()
    {
        return TurnOrder.Count;
    }

    public Player GetPlayerToPreview(int index)
    {
        Player player = TurnOrder[index];
        Logger.Debug($"CombatTurnManager: Getting player at index {index}: {player.PlayerName}", _shouldLog);
        return player;
    }

    public Player GetLastAlly()
    {
        Player lastAlly = AllyTurnOrder[^1];
        Logger.Debug($"CombatTurnManager: Getting last ally: {lastAlly.PlayerName}", _shouldLog);
        return lastAlly;
    }

    public Player SwapTurnToNextPlayer(Player nextPlayer)
    {
        Logger.Debug($"CombatTurnManager: Swapping turn from {CurrentPlayer.PlayerName} to {nextPlayer.PlayerName}",
            _shouldLog);

        EndCurrentPlayerTurn();
        UpdateTurnOrder(nextPlayer);
        SetNewCurrentPlayer(nextPlayer);

        return CurrentPlayer;
    }

    private void EndCurrentPlayerTurn()
    {
        Logger.Debug($"CombatTurnManager: Ending turn for {CurrentPlayer.PlayerName}", _shouldLog);
        CurrentPlayer.EndTurn();
        _turnOrderDisplay.CycleAvatars(CurrentPlayer);
    }

    private void UpdateTurnOrder(Player nextPlayer)
    {
        Logger.Debug("CombatTurnManager: Updating turn order", _shouldLog);
        int nextPlayerIndex = TurnOrder.IndexOf(nextPlayer);

        // Move current player to end
        TurnOrder.Remove(CurrentPlayer);
        TurnOrder.Add(CurrentPlayer);

        // Move any players between current and next to end
        for (int i = 0; i < nextPlayerIndex - 1; i++)
        {
            Player skippedPlayer = TurnOrder[0];
            TurnOrder.RemoveAt(0);
            TurnOrder.Add(skippedPlayer);
        }
    }

    private void SetNewCurrentPlayer(Player nextPlayer)
    {
        Logger.Debug($"CombatTurnManager: Setting new current player to {nextPlayer.PlayerName}", _shouldLog);
        // if nextPlayer.isDead then skip
        CurrentPlayer = nextPlayer;
    }
}
