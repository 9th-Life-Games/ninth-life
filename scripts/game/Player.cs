using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class Player : Node2D
{
    [Signal]
    public delegate void AllyHandEnabledEventHandler(bool enabled);

    [Signal]
    public delegate void EnemyFinishedTurnEventHandler();

    [Signal]
    public delegate void PlayerFinishedDrawingEventHandler();

    private const int HandSize = 4;
    private const float AnimationSpeed = .25f;
    private AnimationPlayer _animationPlayer;
    private Button _attackButton;
    private StringName _combatEntitiesAdded;
    private int _currentHealth;
    private Node2D _defenseDraw;
    private Hand _hand;
    private int _initiativeBonus;

    private int _maxHealth;
    public int CurrentCardPlays { get; private set; }
    public int TotalCardPlays { get; private set; } = 2;
    public int MasteryBonus { get; private set; } = 3;
    public bool IsHandEnabled { get; private set; }
    public Polygon2D PreviewIndicator { get; private set; }
    public Polygon2D AttackModeIndicator { get; private set; }

    public List<Card> Deck { get; } = new();

    public BaseBoard PlayerBoard { get; set; }
    public Polygon2D TurnIndicator { get; private set; }
    public Vector2 BoardActivePosition { get; set; }
    public Vector2 BoardHiddenPosition { get; set; }
    public bool IsAlly { get; set; }
    public int Initiative { get; private set; }
    public string PlayerName { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _hand = GetNode<Hand>("Hand");
        _hand.SetPlayerBoard(PlayerBoard);
        _hand.CardPlayed += OnCardPlayed;
        _defenseDraw = GetNode<Node2D>("../../DefenseDraw");
        _attackButton = GetNode<Button>("../../Attack");

        SetIndicators();

        PlayerBoard.GetNode<Label>("Label").Text = $"{PlayerName}'s Board";

        DrawCards(HandSize, .01);
        PlayerFinishedDrawing += EnablePlayerHand;

        SetupIdleAnimation();
    }

    private void SetupIdleAnimation()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // Play the idle animation
        _animationPlayer.Play("idle");
    }

    public void DrawCardForDefense(Action callback)
    {
        Card drawnCard = Deck[0];
        Deck.RemoveAt(0);
        _defenseDraw.AddChild(drawnCard);
        drawnCard.SetMode(Card.CardMode.Disabled, true);
        GetTree().CreateTimer(1.75).Timeout += () =>
        {
            drawnCard.ExitCard();
            callback.Invoke();
        };
    }

    private void OnCardPlayed()
    {
        CurrentCardPlays++;
        if (CurrentCardPlays == TotalCardPlays && IsAlly)
        {
            SlideHandDisabled(false);
        }
    }


    public void StartTurn()
    {
        DisablePlayerHand();
        TurnIndicator.Visible = true;
        CurrentCardPlays = 0;
        int amountToDraw = HandSize - _hand.GetChildren().Count;
        DrawCards(amountToDraw, .5);

        GetTree().CreateTimer(amountToDraw * .6).Timeout += () =>
        {
            if (!IsAlly)
            {
                EnemyPlayHand();
            }
            else
            {
                _attackButton.Disabled = false;
                _attackButton.ButtonPressed = false;
                EnablePlayerHand();
            }
        };
    }

    public void EndTurn()
    {
        TurnIndicator.Visible = false;
    }

    private void SetIndicators()
    {
        TurnIndicator = GetNode<Polygon2D>("TurnIndicator");
        TurnIndicator.Polygon = new Vector2[] { new(-10, 0), new(10, 0), new(0, -20) };
        TurnIndicator.Rotate(Mathf.DegToRad(180));

        PreviewIndicator = GetNode<Polygon2D>("PreviewIndicator");
        PreviewIndicator.Polygon = new Vector2[] { new(-10, 0), new(10, 0), new(0, -20) };
        PreviewIndicator.Rotate(Mathf.DegToRad(180));

        AttackModeIndicator = GetNode<Polygon2D>("AttackModeIndicator");
        AttackModeIndicator.Polygon = new Vector2[] { new(-10, 0), new(10, 0), new(0, -20) };
        AttackModeIndicator.Rotate(Mathf.DegToRad(180));
    }


    public void ShowPreviewIndicator(bool show)
    {
        PreviewIndicator.Visible = show;
    }

    public void TurnIndicatorPreviewColor(bool inPreviewMode)
    {
        TurnIndicator.Color = !inPreviewMode ? new Color("00ff00") : new Color("006400");
    }

    public void SetAttackModeIndicator(bool inAttackMode)
    {
        AttackModeIndicator.Visible = inAttackMode;
    }

    public void SetSelectedEnemy(bool selected)
    {
        AttackModeIndicator.Color = selected ? Colors.Orange : Colors.Yellow;
    }

    public void EnemyPlayHand()
    {
        List<Card> cardsInHand = _hand.GetChildren().OfType<Card>().ToList();

        // Filter out face cards
        List<Card> nonFaceCards = cardsInHand.Where(card => !card.IsFaceCard).ToList();

        // Group cards by suit
        Dictionary<CardLibrary.SuitType, List<Card>> cardsBySuit = nonFaceCards
            .GroupBy(card => card.SuitType)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Define suit priority (highest to lowest)
        CardLibrary.SuitType[] suitPriority =
        {
            CardLibrary.SuitType.Core, CardLibrary.SuitType.Talent, CardLibrary.SuitType.Weapon,
            CardLibrary.SuitType.Armor
        };

        Card card1 = null;
        Card card2 = null;
        int bestSum = int.MaxValue;

        // Check each suit in priority order
        foreach (CardLibrary.SuitType suitType in suitPriority)
        {
            if (!cardsBySuit.ContainsKey(suitType) || cardsBySuit[suitType].Count < 2)
            {
                continue;
            }

            List<Card> suitCards = cardsBySuit[suitType];

            // Check all pairs of cards in this suit
            for (int i = 0; i < suitCards.Count - 1; i++)
            {
                for (int j = i + 1; j < suitCards.Count; j++)
                {
                    int sum = suitCards[i].NumericValue + suitCards[j].NumericValue;

                    // If sum is >= 10 and less than our current best sum
                    if (sum >= 10 && sum < bestSum)
                    {
                        bestSum = sum;
                        card1 = suitCards[i];
                        card2 = suitCards[j];
                    }
                }
            }

            // If we found a valid pair in this suit, stop looking
            if (card1 != null && card2 != null)
            {
                break;
            }
        }

        // If we found a valid pair, play them
        if (card1 != null && card2 != null)
        {
            Logger.Info(
                $"Playing {card1.NumericValue} and {card2.NumericValue} from {card1.SuitType} suit for total of {bestSum}");
        }
        else
        {
            card1 = cardsInHand[0];
            card2 = cardsInHand[1];

            Logger.Info(
                $"Playing random cards: {card1.NumericValue} from {card1.SuitType} and {card2.NumericValue} from {card2.SuitType}");
        }

        GetTree().CreateTimer(.5).Timeout +=
            () => card1.GetNode<Button>("Button").EmitSignal(BaseButton.SignalName.Pressed);
        GetTree().CreateTimer(1.5).Timeout += () =>
        {
            card2.GetNode<Button>("Button").EmitSignal(BaseButton.SignalName.Pressed);
            EmitSignal(SignalName.EnemyFinishedTurn);
        };
    }

    public void SlideHandIn()
    {
        Tween handTween = GetTree().CreateTween();
        handTween.TweenProperty(
            _hand,
            "global_position:y",
            GlobalPosition.Y + 500,
            AnimationSpeed
        );
        handTween.Finished += delegate
        {
            _hand.PositionCards();
        };
    }

    public void SlideHandOut()
    {
        Tween handTween = GetTree().CreateTween();
        DisablePlayerHand();
        _hand.CollapseHand();
        handTween.TweenProperty(
            _hand,
            "global_position:y",
            GlobalPosition.Y + 850,
            AnimationSpeed
        );
    }

    public void SlideBoardIn()
    {
        Tween boardTween = GetTree().CreateTween();
        boardTween.TweenProperty(
            PlayerBoard,
            "global_position:y",
            GlobalPosition.Y + BoardActivePosition.Y,
            AnimationSpeed
        );
    }

    public void SlideBoardOut()
    {
        Tween boardTween = GetTree().CreateTween();
        boardTween.TweenProperty(
            PlayerBoard,
            "global_position:y",
            GlobalPosition.Y + BoardHiddenPosition.Y,
            AnimationSpeed
        );
    }

    public void SlideHandDisabled(bool emitSignal = true)
    {
        Tween handTween = GetTree().CreateTween();
        handTween.TweenProperty(
            _hand,
            "global_position:y",
            GlobalPosition.Y + 555,
            AnimationSpeed
        );
        handTween.Finished += delegate
        {
            DisablePlayerHand(emitSignal);
            _hand.PositionCards();
        };
    }

    private void ShowBoard()
    {
        Tween boardTween = GetTree().CreateTween();
        boardTween.TweenProperty(
            PlayerBoard,
            "global_position:y",
            GlobalPosition.Y + 650,
            AnimationSpeed
        );
    }

    public void EnablePlayerHand()
    {
        IsHandEnabled = true;
        _hand.EnableCards();
        Logger.Debug($"Enabling {PlayerName}'s hand");
        EmitSignal(SignalName.AllyHandEnabled, true);
    }

    private void DisablePlayerHand(bool emitSignal = true)
    {
        IsHandEnabled = false;
        _hand.DisableCards();
        if (emitSignal)
        {
            EmitSignal(SignalName.AllyHandEnabled, false);
        }
        else
        {
            Logger.Debug("Not emitting signal");
        }
    }

    private async void DrawCards(int x, double delay = AnimationSpeed)
    {
        for (int i = 0; i < x; i++)
        {
            await ToSignal(GetTree().CreateTimer(delay), "timeout");
            DrawCard();
            _hand.PositionCards();
        }

        EmitSignal(SignalName.PlayerFinishedDrawing);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    // Add a card to the player's deck
    private void AddCardToDeck(Card card)
    {
        Deck.Add(card);
    }

    public void CalculateInitiative()
    {
        // Sometimes I want to have better control over the characters turn order for testing
        switch (PlayerName)
        {
            case "Skull":
                _initiativeBonus += 200;
                break;
            case "Hope":
                // _initiativeBonus += 300;
                break;
            case "Goblin":
                // _initiativeBonus += 200;
                break;
            case "Goblin 2":
                // _initiativeBonus += 300;
                break;
        }

        Random rand = new();
        Initiative = rand.Next(100) + 1 + _initiativeBonus;
    }

    // Draw a card from the deck
    private void DrawCard()
    {
        if (Deck.Count <= 0)
        {
            return;
        }

        Card drawnCard = Deck[0];
        Deck.RemoveAt(0);
        AddCardToHand(drawnCard);
    }

    // Add a card to the player's hand
    private void AddCardToHand(Card card)
    {
        _hand.AddChild(card);
    }

    public void ShuffleDeck(int repeat)
    {
        for (int index = 0; index < repeat; index++)
        {
            Random rng = new();

            // Start from the last card and swap it with a random card before it
            for (int i = Deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (Deck[i], Deck[j]) = (Deck[j], Deck[i]);
            }
        }
    }

    public override void _ExitTree()
    {
        foreach (Card card in Deck)
        {
            card.Texture = null;
            card.QueueFree();
        }

        Deck.Clear();

        if (_animationPlayer != null)
        {
            _animationPlayer.Stop();
        }
    }
}
