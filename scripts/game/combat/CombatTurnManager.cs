using System.Collections.Generic;
using System.Linq;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class CombatTurnManager
{
    private readonly List<Player> _allyTurnOrder = new();
    private readonly List<Player> _turnOrder = new();
    public readonly List<Player> EnemyTurnOrder = new();

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
            _turnOrder.Add(player);
            _turnOrderDisplay.AddAvatar(player);
            if (player.IsAlly)
            {
                _allyTurnOrder.Add(player);
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
        return _turnOrder[1];
    }

    public int GetCurrentPlayerIndex()
    {
        return _turnOrder.IndexOf(CurrentPlayer);
    }

    public int GetTurnOrderCount()
    {
        return _turnOrder.Count;
    }

    public Player GetPlayerToPreview(int index)
    {
        return _turnOrder[index];
    }

    public Player GetLastAlly()
    {
        return _allyTurnOrder[^1];
    }

    public Player SwapTurnToNextPlayer(Player nextPlayer)
    {
        CurrentPlayer.EndTurn();
        _turnOrderDisplay.CycleAvatars(CurrentPlayer);
        _turnOrder.Remove(CurrentPlayer);
        _turnOrder.Add(CurrentPlayer);
        CurrentPlayer = nextPlayer;
        return CurrentPlayer;
    }
}
