using System.Collections.Generic;
using System.Linq;
using NinthLife.scripts.game;

namespace NinthLife.scripts.utils;

public class CombatTurnManager
{
    private readonly List<Player> _allyTurnOrder = new();
    private readonly List<Player> _enemyTurnOrder = new();
    private readonly List<Player> _turnOrder = new();
    public Player CurrentPlayer { get; private set; }
    public Player FirstAlly { get; private set; }
    public Player FirstEnemy { get; private set; }

    public void InitializePlayers(CombatManager combatManager)
    {
        foreach (
            Player player in GameUtils.CombatEntities.OrderByDescending(static player =>
                player.Initiative
            )
        )
        {
            CurrentPlayer ??= player;
            _turnOrder.Add(player);
            if (player.IsAlly)
            {
                _allyTurnOrder.Add(player);
            }
            else
            {
                _enemyTurnOrder.Add(player);
            }

            combatManager.AddChild(player);
        }

        CurrentPlayer.TurnIndicator.Visible = true;
    }

    public void SetInitialAlliesAndEnemies(IEnumerable<Player> players)
    {
        foreach (Player player in players)
        {
            if (player.IsAlly)
            {
                FirstAlly ??= player;
            }
            else
            {
                FirstEnemy ??= player;
            }
        }
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
        return _turnOrder.FindLast(player => player.IsAlly);
    }

    public Player SwapTurnToNextPlayer(Player nextPlayer)
    {
        CurrentPlayer.EndTurn();
        _turnOrder.Remove(CurrentPlayer);
        _turnOrder.Add(CurrentPlayer);
        CurrentPlayer = nextPlayer;
        return CurrentPlayer;
    }

    public Player GetFirstEnemyInTurnOrder()
    {
        return _enemyTurnOrder[0];
    }

    public Player GetLastEnemyInTurnOrder()
    {
        return _enemyTurnOrder[^1];
    }

    public Player GetFirstAllyInTurnOrder()
    {
        return _allyTurnOrder[0];
    }

    public Player GetLastAllyInTurnOrder()
    {
        return _allyTurnOrder[^1];
    }
}
