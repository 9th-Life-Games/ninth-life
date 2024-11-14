using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class Hand : Node2D
{
    [Signal]
    public delegate void CardPlayedEventHandler();

    private const float AnimationSpeed = 0.1f;
    private const float CardRepositionDelay = 0.00000001f;
    private readonly bool _shouldLog = false;

    private BaseBoard _playerBoard;

    public override void _Ready()
    {
        Logger.Debug("Hand: Initializing hand", _shouldLog);
        InitializeHand();
    }

    private void InitializeHand()
    {
        ChildEnteredTree += OnChildEnteredTree;
        PositionCards();
    }

    private void OnChildEnteredTree(Node node)
    {
        if (node is Card card)
        {
            Logger.Debug($"Hand: Card entered hand: {card.GetCardName()}", _shouldLog);
            card.CardLeftTree += OnChildExitedTree;
        }
    }

    private void OnChildExitedTree(Card card)
    {
        Logger.Debug($"Hand: Card left hand: {card.GetCardName()}", _shouldLog);
        EmitSignal(SignalName.CardPlayed);
        AddCardToBoard(card);
        ScheduleCardRepositioning();
    }

    private void AddCardToBoard(Card card)
    {
        if (_playerBoard == null)
        {
            Logger.Debug("Hand: Warning: Player board not set", _shouldLog);
            return;
        }

        Logger.Debug($"Hand: Adding {card.GetCardName()} to player board", _shouldLog);
        _playerBoard.AddCard(card);
    }

    private void ScheduleCardRepositioning()
    {
        Logger.Debug("Hand: Scheduling card repositioning", _shouldLog);
        GetTree().CreateTimer(CardRepositionDelay).Timeout += PositionCards;
    }

    public void DisableCards()
    {
        List<Card> cards = GetChildren().OfType<Card>().ToList();
        Logger.Debug($"Hand: Disabling {cards.Count} cards in hand", _shouldLog);

        foreach (Card card in cards)
        {
            Logger.Debug($"Hand: Disabling card: {card.GetCardName()}", _shouldLog);
            card.DisableCard();
        }
    }

    public void EnableCards()
    {
        List<Card> cards = GetChildren().OfType<Card>().ToList();
        Logger.Debug($"Hand: Enabling {cards.Count} cards in hand", _shouldLog);

        foreach (Card card in cards)
        {
            Logger.Debug($"Hand: Enabling card: {card.GetCardName()}", _shouldLog);
            card.EnableCard();
        }
    }

    public void PositionCards()
    {
        Logger.Debug("Hand: Positioning cards in hand", _shouldLog);
        GameUtils.PositionCards(this);
    }

    public void CollapseHand()
    {
        List<Card> cards = GetChildren().OfType<Card>().ToList();
        if (cards.Count == 0)
        {
            Logger.Debug("Hand: No cards to collapse", _shouldLog);
            return;
        }

        Logger.Debug($"Hand: Collapsing {cards.Count} cards in hand", _shouldLog);
        AnimateHandCollapse(cards);
    }

    private void AnimateHandCollapse(List<Card> cards)
    {
        Tween tween = GetTree().CreateTween().SetParallel();

        foreach (Card card in cards)
        {
            Logger.Debug($"Hand: Animating collapse for card: {card.GetCardName()}", _shouldLog);
            CreateCollapseAnimation(tween, card);
        }
    }

    private void CreateCollapseAnimation(Tween tween, Card card)
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

    public override void _ExitTree()
    {
        Logger.Debug("Hand: Cleaning up hand", _shouldLog);
        CleanupCards();
    }

    private void CleanupCards()
    {
        List<Card> cards = GetChildren().OfType<Card>().ToList();
        Logger.Debug($"Hand: Cleaning up {cards.Count} cards", _shouldLog);

        foreach (Card card in cards)
        {
            Logger.Debug($"Hand: Freeing card: {card.GetCardName()}", _shouldLog);
            card.Texture = null;
            card.QueueFree();
        }
    }

    public void SetPlayerBoard(BaseBoard baseBoard)
    {
        Logger.Debug("Hand: Setting player board reference", _shouldLog);
        _playerBoard = baseBoard;
    }
}
