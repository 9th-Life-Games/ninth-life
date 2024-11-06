using System.Collections.Generic;
using System.Linq;
using NinthLife.scripts.game;

namespace NinthLife.scripts.utils;

public class CombatTurnManager
{
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
}
