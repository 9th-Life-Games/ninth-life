using System;
using System.Collections.Generic;
using Godot;

namespace NinthLife.scripts.game;

public partial class Player : Node2D
{
    [Signal]
    public delegate void PlayerTurnEndedEventHandler();

    [Signal]
    public delegate void PlayerTurnStartedEventHandler();


    private const int HandSize = 9;
    private const float AnimationSpeed = .25f;
    private StringName _combatEntitiesAdded;
    private int _currentCardPlays;
    private Hand _hand;
    private int _totalCardPlays = 2;

    public List<Card> Deck { get; } = new();

    public List<Card> Discard { get; } = new();
    public bool IsAlly { get; set; }
    public int Initiative { get; private set; }
    public int InitiativeBonus { get; set; }
    public string PlayerName { get; set; }

    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _hand = GetNode<Hand>("Hand");
        DrawCards(HandSize, .01);
    }

    public void StartTurn()
    {
        GD.Print("Player turn started");
        var amountToDraw = HandSize - _hand.GetChildren().Count;
        DrawCards(amountToDraw, .5);
        EmitSignal(SignalName.PlayerTurnStarted);
    }

    public void EndTurn()
    {
        GD.Print("Player turn ended");
        EmitSignal(SignalName.PlayerTurnEnded);
    }

    public void SlideHandIn()
    {
        GD.Print("Sliding hand in");
        var tween = GetTree().CreateTween();
        tween.TweenProperty(_hand, "global_position:y", GlobalPosition.Y + 500, AnimationSpeed);
        tween.Finished += delegate
        {
            _hand.PositionCards();
            _hand.EnableCards();
        };
    }

    public void SlideHandOut()
    {
        var tween = GetTree().CreateTween();
        _hand.DisableCards();
        _hand.CollapseHand();
        tween.TweenProperty(_hand, "global_position:y", GlobalPosition.Y + 750, AnimationSpeed);
    }

    public void SlideHandDisabled()
    {
        var tween = GetTree().CreateTween();
        tween.TweenProperty(_hand, "global_position:y", GlobalPosition.Y + 575, AnimationSpeed);
        tween.Finished += delegate
        {
            _hand.DisableCards();
            _hand.PositionCards();
        };
    }

    public async void DrawCards(int x, double delay = AnimationSpeed)
    {
        for (var i = 0; i < x; i++)
        {
            await ToSignal(GetTree().CreateTimer(delay), "timeout");
            DrawCard();
            _hand.PositionCards();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    // Add a card to the player's deck
    public void AddCardToDeck(Card card)
    {
        Deck.Add(card);
    }

    public void CalculateInitiative()
    {
        var rand = new Random();
        Initiative = rand.Next(100) + 1 + InitiativeBonus;
    }

    // Draw a card from the deck
    public void DrawCard()
    {
        if (Deck.Count <= 0) return;

        var drawnCard = Deck[0];
        Deck.RemoveAt(0);
        GD.Print("Adding to hand: ", drawnCard.SuitType, " ", drawnCard.NumericValue, " ",
            drawnCard.Texture.ResourcePath, " Parent: ", drawnCard.GetParent());
        AddCardToHand(drawnCard);
    }

    // Add a card to the player's hand
    public void AddCardToHand(Card card)
    {
        // GD.Print(card.GetParent());
        if (card.GetParent() != null)
            GD.Print("Fuck you! ", card.SuitType, " ", card.NumericValue, " ",
                card.Texture.ResourcePath, " Parent: ", card.GetParent());
        _hand.AddChild(card);
    }

    // Add a card to the player's discard
    public void AddCardToDiscard(Card card)
    {
        Discard.Add(card);
    }

    public void ShuffleDeck(int repeat)
    {
        for (var index = 0; index < repeat; index++)
        {
            var rng = new Random();

            // Start from the last card and swap it with a random card before it
            for (var i = Deck.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (Deck[i], Deck[j]) = (Deck[j], Deck[i]);
            }
        }
    }

    public override void _ExitTree()
    {
        foreach (var card in Deck) card.QueueFree();
    }
}