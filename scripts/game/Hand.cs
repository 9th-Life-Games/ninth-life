using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class Hand : Node2D
{
    [Signal]
    public delegate void CardPlayedEventHandler();

    private const float AnimationSpeed = .1f;

    private BaseBoard _playerBoard;

    public override void _Ready()
    {
        ChildEnteredTree += OnChildEnteredTree;
        PositionCards();
    }

    private void OnChildEnteredTree(Node node)
    {
        ((Card)node).CardLeftTree += OnChildExitedTree;
    }

    private void OnChildExitedTree(Card card)
    {
        EmitSignal(SignalName.CardPlayed);
        Logger.Debug("Adding card to player board***");
        _playerBoard.AddCard(card);
        GetTree().CreateTimer(.00000001).Timeout += PositionCards;
    }

    public void DisableCards()
    {
        foreach (Card card in GetChildren().OfType<Card>())
        {
            card.DisableCard();
        }
    }

    public void EnableCards()
    {
        foreach (Card card in GetChildren().OfType<Card>())
        {
            card.EnableCard();
        }
    }

    public void PositionCards()
    {
        GameUtils.PositionCards(this);
    }

    public void CollapseHand()
    {
        List<Card> children = GetChildren().OfType<Card>().ToList();
        if (children.Count == 0)
        {
            return; // Don't create tween if no cards
        }

        Tween tween = GetTree().CreateTween().SetParallel();

        foreach (Card card in children)
        {
            tween.TweenProperty(card, "rotation_degrees", 0, AnimationSpeed);
            tween.TweenProperty(
                card,
                "global_position:x",
                GlobalPosition.X,
                AnimationSpeed
            );
            tween.TweenProperty(
                card,
                "global_position:y",
                GlobalPosition.Y,
                AnimationSpeed
            );
        }
    }

    public override void _ExitTree()
    {
        foreach (Card card in GetChildren().OfType<Card>())
        {
            card.Texture = null;
            card.QueueFree();
        }
    }

    public void SetPlayerBoard(BaseBoard baseBoard)
    {
        _playerBoard = baseBoard;
    }
}
