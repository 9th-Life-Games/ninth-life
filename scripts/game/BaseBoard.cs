using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class BaseBoard : Node2D
{
    [Signal]
    public delegate void StackActionEventHandler(string suitName);

    private const float AnimationSpeed = 0.1f;
    private const float DiscardDelay = 0.5f;
    private const float InPlayCardSpacing = 84f; // Total width from -47 to 47
    private const float InPlayCardStartX = -47f;

    private readonly Dictionary<CardLibrary.SuitType, List<Card>> _cardPiles = new()
    {
        { CardLibrary.SuitType.Weapon, new List<Card>() },
        { CardLibrary.SuitType.Armor, new List<Card>() },
        { CardLibrary.SuitType.Core, new List<Card>() },
        { CardLibrary.SuitType.Talent, new List<Card>() }
    };

    private readonly bool _shouldLog = true;

    private Sprite2D _armorBox;
    private Sprite2D _coreBox;
    private Sprite2D _discardBox;
    private Sprite2D _inPlayBox;
    private Sprite2D _talentBox;
    private Sprite2D _weaponBox;

    public List<Card> WeaponCards => _cardPiles[CardLibrary.SuitType.Weapon];
    public List<Card> ArmorCards => _cardPiles[CardLibrary.SuitType.Armor];
    public List<Card> CoreCards => _cardPiles[CardLibrary.SuitType.Core];
    public List<Card> TalentCards => _cardPiles[CardLibrary.SuitType.Talent];

    public override void _Ready()
    {
        InitializeBoxes();
        SetupButtonHandlers();
    }

    private void InitializeBoxes()
    {
        Logger.Debug("BaseBoard: Initializing board boxes", _shouldLog);
        _discardBox = GetNode<Sprite2D>("DiscardBox");
        _armorBox = GetNode<Sprite2D>("ArmorBox");
        _coreBox = GetNode<Sprite2D>("CoreBox");
        _inPlayBox = GetNode<Sprite2D>("InPlayBox");
        _talentBox = GetNode<Sprite2D>("TalentBox");
        _weaponBox = GetNode<Sprite2D>("WeaponBox");
    }

    private void SetupButtonHandlers()
    {
        Logger.Debug("BaseBoard: Setting up button handlers", _shouldLog);
        _discardBox.GetNode<Button>("Button").Pressed += () => OnButtonPressed(_discardBox);
        _inPlayBox.GetNode<Button>("Button").Pressed += () => OnButtonPressed(_inPlayBox);
    }

    private void OnButtonPressed(Sprite2D cardBox)
    {
        Logger.Debug($"BaseBoard: Displaying cards for {cardBox.Name}", _shouldLog);
        DisplayCards displayCards = ResourceManager.Load<PackedScene>("res://scenes/display_cards.tscn")
            .Instantiate<DisplayCards>();
        displayCards.SetCardsRoot(cardBox);
        displayCards.SetIsAlly(GetName() == "PlayerBoard");
        GetTree().GetRoot().GetNode<Node2D>("Game").AddChild(displayCards);
    }

    public void AddCard(Card card, bool discard = false)
    {
        if (discard)
        {
            Logger.Debug($"BaseBoard: Adding card to discard: {card.GetCardName()}", _shouldLog);
            _discardBox.AddChild(card);
            return;
        }

        if (card.NumericValue >= 19)
        {
            HandleFaceCard(card);
            return;
        }

        HandleNormalCard(card);
    }

    private void HandleFaceCard(Card card)
    {
        Logger.Debug($"BaseBoard: Adding face card to in-play: {card.GetCardName()}", _shouldLog);
        Card inPlayCard = card.DuplicateCard(Card.CardMode.Disabled, true);
        _inPlayBox.AddChild(inPlayCard);
        PositionInPlayCards();
    }

    private void HandleNormalCard(Card card)
    {
        Logger.Debug($"BaseBoard: Processing normal card: {card.GetCardName()}", _shouldLog);
        _cardPiles[card.SuitType].Add(card);
        List<Card> cardsToMove = _cardPiles[card.SuitType].ToList();
        ProcessCardBySuitType(card, cardsToMove);
    }

    private void ProcessCardBySuitType(Card card, List<Card> cardsToMove)
    {
        ProgressBar progressBar = GetProgressBarForSuitType(card.SuitType);
        progressBar.Value += card.NumericValue;

        Logger.Debug($"BaseBoard: Progress for {card.SuitType}: {progressBar.Value}/{progressBar.MaxValue}",
            _shouldLog);

        if (progressBar.Value >= progressBar.MaxValue)
        {
            Logger.Debug(
                $"BaseBoard: Progress bar full for {card.SuitType} card {card.SuitName}, moving cards to discard",
                _shouldLog);
            EmitSignal(SignalName.StackAction, card.SuitName);

            AddCardsToDiscardSequentially(cardsToMove, card.SuitType);
            progressBar.Value = 0;
        }
    }

    private ProgressBar GetProgressBarForSuitType(CardLibrary.SuitType suitType)
    {
        return suitType switch
        {
            CardLibrary.SuitType.Weapon => _weaponBox.GetNode<ProgressBar>("ProgressBar"),
            CardLibrary.SuitType.Armor => _armorBox.GetNode<ProgressBar>("ProgressBar"),
            CardLibrary.SuitType.Core => _coreBox.GetNode<ProgressBar>("ProgressBar"),
            CardLibrary.SuitType.Talent => _talentBox.GetNode<ProgressBar>("ProgressBar"),
            _ => throw new ArgumentException($"Unexpected suit type: {suitType}")
        };
    }

    private void AddCardsToDiscardSequentially(List<Card> cards, CardLibrary.SuitType suitType)
    {
        Logger.Debug($"BaseBoard: Adding {cards.Count} cards to discard for {suitType}", _shouldLog);
        for (int i = 0; i < cards.Count; i++)
        {
            Card cardToDiscard = cards[i];
            _cardPiles[suitType].Remove(cardToDiscard);

            Card discardedCard = cardToDiscard.DuplicateCard(Card.CardMode.Disabled, true);

            GetTree().CreateTimer(DiscardDelay * i).Timeout += () =>
            {
                Logger.Debug($"BaseBoard: Adding card to discard: {discardedCard.GetCardName()}", _shouldLog);
                _discardBox.AddChild(discardedCard);
            };
        }
    }

    public void RemoveCard(Card card, CardLibrary.SuitType suitType)
    {
        Logger.Debug($"BaseBoard: Removing card from {suitType} pile: {card.GetCardName()}", _shouldLog);
        _cardPiles[suitType].Remove(card);
    }

    private int GetPileValue(CardLibrary.SuitType suitType)
    {
        int value = _cardPiles[suitType].Sum(card => card.NumericValue);
        Logger.Debug($"BaseBoard: Total value for {suitType} pile: {value}", _shouldLog);
        return value;
    }

    private void PositionInPlayCards()
    {
        List<Card> cards = _inPlayBox.GetChildren().OfType<Card>().ToList();
        Logger.Debug($"BaseBoard: Positioning {cards.Count} in-play cards", _shouldLog);

        if (cards.Count == 0)
        {
            return;
        }

        switch (cards.Count)
        {
            case 1:
                PositionSingleCard(cards[0]);
                break;
            case 2:
                PositionTwoCards(cards);
                break;
            default:
                PositionMultipleCards(cards);
                break;
        }
    }

    private void PositionSingleCard(Card card)
    {
        Logger.Debug("BaseBoard: Positioning single in-play card", _shouldLog);
        card.Position = new Vector2(InPlayCardStartX, 0);
    }

    private void PositionTwoCards(List<Card> cards)
    {
        Logger.Debug("BaseBoard: Positioning two in-play cards", _shouldLog);
        cards[0].Position = new Vector2(InPlayCardStartX, 0);
        cards[1].Position = new Vector2(-InPlayCardStartX, 0);
    }

    private void PositionMultipleCards(List<Card> cards)
    {
        Logger.Debug($"BaseBoard: Positioning {cards.Count} in-play cards", _shouldLog);
        float spacing = InPlayCardSpacing / (cards.Count - 1);

        for (int i = 0; i < cards.Count - 1; i++)
        {
            float xPos = InPlayCardStartX + (spacing * i);
            Tween tween = GetTree().CreateTween().SetParallel();
            tween.TweenProperty(cards[i], "position", new Vector2(xPos, 0), AnimationSpeed);
        }

        float lastXPos = InPlayCardStartX + (spacing * (cards.Count - 1));
        cards[^1].Position = new Vector2(lastXPos, 0);
    }

    public override void _ExitTree()
    {
        Logger.Debug("BaseBoard: Cleaning up board resources", _shouldLog);
        foreach (List<Card> pile in _cardPiles.Values)
        {
            foreach (Card card in pile)
            {
                card.Texture = null;
                card.QueueFree();
            }

            pile.Clear();
        }

        _cardPiles.Clear();
    }
}
