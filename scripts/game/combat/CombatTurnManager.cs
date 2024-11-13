using System.Collections.Generic;
using System.Linq;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class CombatTurnManager
{
    public readonly List<Player> AllyTurnOrder = new();
    public readonly List<Player> EnemyTurnOrder = new();
    public readonly List<Player> TurnOrder = new();

    private TurnOrderDisplay _turnOrderDisplay;
    public Player CurrentPlayer { get; private set; }

    public void InitializePlayers(CombatManager combatManager, TurnOrderDisplay turnOrderDisplay)
    {
        _turnOrderDisplay = turnOrderDisplay;
        foreach (
            Player player in GameUtils.CombatEntities.OrderByDescending(static player =>
                player.Initiative
            )
        )
        {
            CurrentPlayer ??= player;
            TurnOrder.Add(player);
            _turnOrderDisplay.AddAvatar(player);
            if (player.IsAlly)
            {
                AllyTurnOrder.Add(player);
            }
            else
            {
                EnemyTurnOrder.Add(player);
            }

            combatManager.AddChild(player);
        }

        CurrentPlayer.TurnIndicator.Visible = true;
    }

    public Player GetNextPlayer()
    {
        return TurnOrder[1];
    }

    public int GetCurrentPlayerIndex()
    {
        return TurnOrder.IndexOf(CurrentPlayer);
    }

    public int GetPlayerIndex(Player player)
    {
        return TurnOrder.IndexOf(player);
    }

    public int GetTurnOrderCount()
    {
        return TurnOrder.Count;
    }

    public Player GetPlayerToPreview(int index)
    {
        return TurnOrder[index];
    }

    public Player GetLastAlly()
    {
        return AllyTurnOrder[^1];
    }

    public Player SwapTurnToNextPlayer(Player nextPlayer)
    {
        CurrentPlayer.EndTurn();
        _turnOrderDisplay.CycleAvatars(CurrentPlayer);
        TurnOrder.Remove(CurrentPlayer);
        TurnOrder.Add(CurrentPlayer);
        CurrentPlayer = nextPlayer;
        return CurrentPlayer;
    }
}
