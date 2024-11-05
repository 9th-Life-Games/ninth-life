using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class BaseBoard : Node2D
{
    private readonly Dictionary<CardLibrary.SuitType, List<Card>> _cardPiles = new()
    {
        { CardLibrary.SuitType.Weapon, new List<Card>() },
        { CardLibrary.SuitType.Armor, new List<Card>() },
        { CardLibrary.SuitType.Core, new List<Card>() },
        { CardLibrary.SuitType.Talent, new List<Card>() }
    };

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
        _discardBox = GetNode<Sprite2D>("DiscardBox");
        _armorBox = GetNode<Sprite2D>("ArmorBox");
        _coreBox = GetNode<Sprite2D>("CoreBox");
        _inPlayBox = GetNode<Sprite2D>("InPlayBox");
        _talentBox = GetNode<Sprite2D>("TalentBox");
        _weaponBox = GetNode<Sprite2D>("WeaponBox");

        _discardBox.GetNode<Button>("Button").Pressed += () => OnButtonPressed(_discardBox);
        _inPlayBox.GetNode<Button>("Button").Pressed += () => OnButtonPressed(_inPlayBox);
    }

    private void OnButtonPressed(Sprite2D cardBox)
    {
        DisplayCards displayCards = GD.Load<PackedScene>("res://scenes/display_cards.tscn").Instantiate<DisplayCards>();
        displayCards.SetCardsRoot(cardBox);
        displayCards.SetIsAlly(GetName() == "PlayerBoard");
        GetTree().GetRoot().GetNode<Node2D>("Game").AddChild(displayCards);
    }

    public void AddCard(Card card)
    {
        if (card.NumericValue < 19)
        {
            _cardPiles[card.SuitType].Add(card);
            List<Card> cardsToMove = _cardPiles[card.SuitType].ToList();
            switch (card.SuitType)
            {
                case CardLibrary.SuitType.Weapon:
                    ProgressBar weaponProgressBar = _weaponBox.GetNode<ProgressBar>("ProgressBar");
                    weaponProgressBar.Value += card.NumericValue;
                    if (weaponProgressBar.Value >= weaponProgressBar.MaxValue)
                    {
                        AddCardsToDiscardSequentially(cardsToMove, card.SuitType);
                        weaponProgressBar.Value = 0;
                    }

                    break;
                case CardLibrary.SuitType.Armor:
                    ProgressBar armorProgressBar = _armorBox.GetNode<ProgressBar>("ProgressBar");
                    armorProgressBar.Value += card.NumericValue;
                    if (armorProgressBar.Value >= armorProgressBar.MaxValue)
                    {
                        AddCardsToDiscardSequentially(cardsToMove, card.SuitType);
                        armorProgressBar.Value = 0;
                    }

                    break;
                case CardLibrary.SuitType.Core:
                    ProgressBar coreProgressBar = _coreBox.GetNode<ProgressBar>("ProgressBar");
                    coreProgressBar.Value += card.NumericValue;
                    if (coreProgressBar.Value >= coreProgressBar.MaxValue)
                    {
                        AddCardsToDiscardSequentially(cardsToMove, card.SuitType);
                        coreProgressBar.Value = 0;
                    }

                    break;
                case CardLibrary.SuitType.Talent:
                    ProgressBar talentProgressBar = _talentBox.GetNode<ProgressBar>("ProgressBar");
                    talentProgressBar.Value += card.NumericValue;
                    if (talentProgressBar.Value >= talentProgressBar.MaxValue)
                    {
                        AddCardsToDiscardSequentially(cardsToMove, card.SuitType);
                        // Reset progress bar
                        talentProgressBar.Value = 0;
                    }

                    break;
            }
        }
        else
        {
            Card inPlayCard = card.DuplicateCard(false, true);
            _inPlayBox.AddChild(inPlayCard);
            PositionInPlayCards();
        }
    }

    private void AddCardsToDiscardSequentially(List<Card> cards, CardLibrary.SuitType suitType)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Card cardToDiscard = cards[i];
            _cardPiles[suitType].Remove(cardToDiscard);

            // Capture the card in a local variable for the closure
            Card discardedCard = cardToDiscard.DuplicateCard(false, true);

            // Create sequential delays
            GetTree().CreateTimer(0.5f * i).Timeout += () =>
            {
                _discardBox.AddChild(discardedCard);
            };
        }
    }

    public void RemoveCard(Card card, CardLibrary.SuitType suitType)
    {
        _cardPiles[suitType].Remove(card);
    }

    private int GetPileValue(CardLibrary.SuitType suitType)
    {
        return _cardPiles[suitType].Sum(card => card.NumericValue);
    }

    private void PositionInPlayCards()
    {
        List<Card> cards = _inPlayBox.GetChildren().OfType<Card>().ToList();

        Tween tween = GetTree().CreateTween().SetParallel();
        const float animationSpeed = 0.1f;

        switch (cards.Count)
        {
            case 0:
                return;
            case 1:
                cards[0].Position = new Vector2(-47, 0);
                return;
            case 2:
                cards[0].Position = new Vector2(-47, 0);
                cards[1].Position = new Vector2(47, 0);
                return;
        }

        // For 3 or more cards, spread them evenly
        const float totalWidth = 84; // -47 to 47
        float spacing = totalWidth / (cards.Count - 1);

        // Tween all cards except the last one
        for (int i = 0; i < cards.Count - 1; i++)
        {
            float xPos = -47 + (spacing * i);
            tween.TweenProperty(cards[i], "position", new Vector2(xPos, 0), animationSpeed);
        }

        // Position the last card directly without tweening
        float lastXPos = -47 + (spacing * (cards.Count - 1));
        cards[^1].Position = new Vector2(lastXPos, 0);
    }
}
