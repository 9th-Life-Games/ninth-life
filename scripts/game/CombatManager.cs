using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class CombatManager : Node2D
{
    [Signal]
    public delegate void CombatEntitiesAddedEventHandler();

    private readonly List<Player> _turnOrder = new();

    private Player _firstAlly;

    public Player CurrentPlayer;

    public override void _Ready()
    {
        foreach (var player in GameUtils.CombatEntities.OrderByDescending(player => player.Initiative))
        {
            CurrentPlayer ??= player;
            _turnOrder.Add(player);
            AddChild(player);
        }

        EmitSignal(SignalName.CombatEntitiesAdded);

        if (CurrentPlayer.IsAlly)
        {
            CurrentPlayer.SlideHandIn();
        }
        else
        {
            foreach (var player in GetChildren().OfType<Player>())
                if (player.IsAlly)
                    _firstAlly ??= player;
            _firstAlly.SlideHandDisabled();
        }
    }

    public void NextTurn()
    {
        var nextPlayer = _turnOrder[1];
        if (!nextPlayer.IsAlly)
            CurrentPlayer.SlideHandDisabled();
        else
            CurrentPlayer.SlideHandOut();
        if (!CurrentPlayer.IsAlly && nextPlayer.IsAlly)
            for (var i = _turnOrder.Count - 1; i >= 0; i--)
            {
                if (!_turnOrder[i].IsAlly) continue;
                _turnOrder[i].SlideHandOut();
                break;
            }

        CurrentPlayer.EndTurn();
        _turnOrder.Remove(CurrentPlayer);
        _turnOrder.Add(CurrentPlayer);
        CurrentPlayer = nextPlayer;
        if (CurrentPlayer.IsAlly) CurrentPlayer.SlideHandIn();
        CurrentPlayer.StartTurn();
    }
}