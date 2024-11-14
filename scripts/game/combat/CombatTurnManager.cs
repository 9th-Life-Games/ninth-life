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
        Logger.Debug("Initializing combat turn manager", _shouldLog);
        _turnOrderDisplay = turnOrderDisplay;

        List<Player> orderedPlayers = GameUtils.CombatEntities
            .OrderByDescending(static player => player.Initiative)
            .ToList();

        Logger.Debug("Setting up turn order with players:", _shouldLog);
        foreach (Player player in orderedPlayers)
        {
            ProcessPlayer(player, combatManager);
        }

        InitializeFirstTurn();
    }

    private void ProcessPlayer(Player player, CombatManager combatManager)
    {
        Logger.Debug($"Processing player: {player.PlayerName} (Initiative: {player.Initiative})", _shouldLog);

        if (CurrentPlayer == null)
        {
            CurrentPlayer = player;
            Logger.Debug($"Set initial current player: {player.PlayerName}", _shouldLog);
        }

        TurnOrder.Add(player);
        _turnOrderDisplay.AddAvatar(player);

        if (player.IsAlly)
        {
            Logger.Debug($"Adding {player.PlayerName} to ally turn order", _shouldLog);
            AllyTurnOrder.Add(player);
        }
        else
        {
            Logger.Debug($"Adding {player.PlayerName} to enemy turn order", _shouldLog);
            EnemyTurnOrder.Add(player);
        }

        combatManager.AddChild(player);
    }

    private void InitializeFirstTurn()
    {
        Logger.Debug($"Initializing first turn for {CurrentPlayer.PlayerName}", _shouldLog);
        CurrentPlayer.TurnIndicator.Visible = true;
    }

    public Player GetNextPlayer()
    {
        Player nextPlayer = TurnOrder[1];
        Logger.Debug($"Next player will be: {nextPlayer.PlayerName}", _shouldLog);
        return nextPlayer;
    }

    public int GetCurrentPlayerIndex()
    {
        int index = TurnOrder.IndexOf(CurrentPlayer);
        Logger.Debug($"Current player {CurrentPlayer.PlayerName} index: {index}", _shouldLog);
        return index;
    }

    public int GetPlayerIndex(Player player)
    {
        int index = TurnOrder.IndexOf(player);
        Logger.Debug($"Player {player.PlayerName} index: {index}", _shouldLog);
        return index;
    }

    public int GetTurnOrderCount()
    {
        return TurnOrder.Count;
    }

    public Player GetPlayerToPreview(int index)
    {
        Player player = TurnOrder[index];
        Logger.Debug($"Getting player at index {index}: {player.PlayerName}", _shouldLog);
        return player;
    }

    public Player GetLastAlly()
    {
        Player lastAlly = AllyTurnOrder[^1];
        Logger.Debug($"Getting last ally: {lastAlly.PlayerName}", _shouldLog);
        return lastAlly;
    }

    public Player SwapTurnToNextPlayer(Player nextPlayer)
    {
        Logger.Debug($"Swapping turn from {CurrentPlayer.PlayerName} to {nextPlayer.PlayerName}", _shouldLog);

        EndCurrentPlayerTurn();
        UpdateTurnOrder();
        SetNewCurrentPlayer(nextPlayer);

        return CurrentPlayer;
    }

    private void EndCurrentPlayerTurn()
    {
        Logger.Debug($"Ending turn for {CurrentPlayer.PlayerName}", _shouldLog);
        CurrentPlayer.EndTurn();
        _turnOrderDisplay.CycleAvatars(CurrentPlayer);
    }

    private void UpdateTurnOrder()
    {
        Logger.Debug("Updating turn order", _shouldLog);
        TurnOrder.Remove(CurrentPlayer);
        TurnOrder.Add(CurrentPlayer);
    }

    private void SetNewCurrentPlayer(Player nextPlayer)
    {
        Logger.Debug($"Setting new current player to {nextPlayer.PlayerName}", _shouldLog);
        CurrentPlayer = nextPlayer;
    }
}
