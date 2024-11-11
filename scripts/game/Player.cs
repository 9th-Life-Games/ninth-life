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

    private const float AnimationSpeed = .25f;

    private StringName _combatEntitiesAdded;
    private int _currentHealth;
    private int _initiativeBonus;
    private int _maxHealth;

    public int HandSize { get; private set; } = 4;

    // Properties
    public Hand Hand { get; private set; }

    public int CurrentCardPlays { get; private set; }
    public bool HasAttacked { get; private set; }
    public int TotalCardPlays { get; private set; } = 2;
    public int MasteryBonus { get; private set; } = 3;
    public bool IsHandEnabled { get; private set; }
    public List<Card> Deck { get; } = new();
    public BaseBoard PlayerBoard { get; set; }
    public Polygon2D TurnIndicator { get; private set; }
    public Polygon2D PreviewIndicator { get; private set; }
    public Polygon2D AttackModeIndicator { get; private set; }
    public Vector2 BoardActivePosition { get; set; }
    public Vector2 BoardHiddenPosition { get; set; }
    public bool IsAlly { get; set; }
    public int Initiative { get; private set; }
    public string PlayerName { get; set; }

    public override void _Ready()
    {
        InitializeComponents();
        SetupIndicators();
        InitializePlayerBoard();
    }

    private void InitializeComponents()
    {
        Hand = GetNode<Hand>("Hand");
        Hand.SetPlayerBoard(PlayerBoard);
        Hand.CardPlayed += OnCardPlayed;
    }

    private void InitializePlayerBoard()
    {
        PlayerBoard.GetNode<Label>("Label").Text = $"{PlayerName}'s Board";
    }

    public void DrawCardForDefense(Action callback)
    {
        if (Deck.Count <= 0)
        {
            return;
        }

        Card drawnCard = Deck[0];
        Deck.RemoveAt(0);
        drawnCard.SetMode(Card.CardMode.Disabled, true);
        GetTree().CreateTimer(1.75).Timeout += () =>
        {
            drawnCard.ExitCard();
            callback.Invoke();
        };
    }

    private void OnCardPlayed(int cardsPlayed)
    {
        CurrentCardPlays = cardsPlayed;
        if (cardsPlayed == TotalCardPlays)
        {
            SlideHandDisabled(false);
        }
    }

    public void StartTurn()
    {
        ResetTurnState();
        TurnIndicator.Visible = true;
        int amountToDraw = HandSize - Hand.GetChildren().Count;
        DrawCards(amountToDraw, .5);
    }

    public void EndTurn()
    {
        TurnIndicator.Visible = false;
        ResetTurnState();
    }

    private void ResetTurnState()
    {
        HasAttacked = false;
        CurrentCardPlays = 0;
        Hand?.ResetPlays(); // Make sure Hand class has this method
        DisablePlayerHand();
    }

    // Optional - a method to set HasAttacked that can be called from CombatPhaseState
    public void SetHasAttacked(bool value)
    {
        HasAttacked = value;
        if (value)
        {
            // Maybe trigger some visual feedback or state changes when attack is completed
            SlideHandDisabled();
        }
    }

    private void SetupIndicators()
    {
        SetupTurnIndicator();
        SetupPreviewIndicator();
        SetupAttackModeIndicator();
    }

    private void SetupTurnIndicator()
    {
        TurnIndicator = GetNode<Polygon2D>("TurnIndicator");
        SetupIndicatorPolygon(TurnIndicator);
    }

    private void SetupPreviewIndicator()
    {
        PreviewIndicator = GetNode<Polygon2D>("PreviewIndicator");
        SetupIndicatorPolygon(PreviewIndicator);
    }

    private void SetupAttackModeIndicator()
    {
        AttackModeIndicator = GetNode<Polygon2D>("AttackModeIndicator");
        SetupIndicatorPolygon(AttackModeIndicator);
    }

    private static void SetupIndicatorPolygon(Polygon2D indicator)
    {
        indicator.Polygon = new Vector2[] { new(-10, 0), new(10, 0), new(0, -20) };
        indicator.Rotate(Mathf.DegToRad(180));
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

    public void CalculateInitiative()
    {
        _initiativeBonus = PlayerName switch
        {
            "Skull" => _initiativeBonus + 200,
            _ => _initiativeBonus
        };

        Random rand = new();
        Initiative = rand.Next(100) + 1 + _initiativeBonus;
    }

    public void ShuffleDeck(int repeat)
    {
        Random rng = new();
        for (int index = 0; index < repeat; index++)
        {
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
    }

    #region Hand Management

    public void SlideHandIn()
    {
        CreateHandTween(GlobalPosition.Y + 500, () => Hand.PositionCards());
    }

    public void SlideHandOut()
    {
        DisablePlayerHand();
        Hand.CollapseHand();
        CreateHandTween(GlobalPosition.Y + 850);
    }

    public void SlideHandDisabled(bool emitSignal = true)
    {
        CreateHandTween(GlobalPosition.Y + 555, () =>
        {
            DisablePlayerHand(emitSignal);
            Hand.PositionCards();
        });
    }

    private void CreateHandTween(float targetY, Action onComplete = null)
    {
        Tween handTween = GetTree().CreateTween();
        handTween.TweenProperty(Hand, "global_position:y", targetY, AnimationSpeed);
        if (onComplete != null)
        {
            handTween.Finished += onComplete;
        }
    }

    #endregion

    #region Board Management

    public void SlideBoardIn()
    {
        CreateBoardTween(GlobalPosition.Y + BoardActivePosition.Y);
    }

    public void SlideBoardOut()
    {
        CreateBoardTween(GlobalPosition.Y + BoardHiddenPosition.Y);
    }

    private void CreateBoardTween(float targetY)
    {
        Tween boardTween = GetTree().CreateTween();
        boardTween.TweenProperty(PlayerBoard, "global_position:y", targetY, AnimationSpeed);
    }

    #endregion

    #region Card Management

    public void EnablePlayerHand()
    {
        IsHandEnabled = true;
        Hand.EnableCards();
        EmitSignal(SignalName.AllyHandEnabled, true);
    }

    public void DisablePlayerHand(bool emitSignal = true)
    {
        IsHandEnabled = false;
        Hand.DisableCards();
        if (emitSignal)
        {
            EmitSignal(SignalName.AllyHandEnabled, false);
        }
    }

    public async void DrawCards(int amount, double delay = AnimationSpeed)
    {
        for (int i = 0; i < amount; i++)
        {
            await ToSignal(GetTree().CreateTimer(delay), "timeout");
            DrawCard();
            Hand.PositionCards();
        }

        EmitSignal(SignalName.PlayerFinishedDrawing);
    }

    private void DrawCard()
    {
        if (Deck.Count <= 0)
        {
            return;
        }

        Card drawnCard = Deck[0];
        Deck.RemoveAt(0);
        Hand.AddChild(drawnCard);
    }

    #endregion

    #region Combat Logic

    public void EnemyPlayHand()
    {
        (Card card1, Card card2) cardPairs = FindBestCardPair();
        PlayCardPair(cardPairs.card1, cardPairs.card2);
    }

    private (Card card1, Card card2) FindBestCardPair()
    {
        List<Card> cardsInHand = Hand.GetChildren().OfType<Card>().ToList();
        List<Card> nonFaceCards = cardsInHand.Where(card => !card.IsFaceCard).ToList();

        Dictionary<CardLibrary.SuitType, List<Card>> cardsBySuit = nonFaceCards
            .GroupBy(card => card.SuitType)
            .ToDictionary(g => g.Key, g => g.ToList());

        CardLibrary.SuitType[] suitPriority =
        {
            CardLibrary.SuitType.Core, CardLibrary.SuitType.Talent, CardLibrary.SuitType.Weapon,
            CardLibrary.SuitType.Armor
        };

        return FindOptimalCardPair(cardsBySuit, suitPriority, cardsInHand);
    }

    private (Card card1, Card card2) FindOptimalCardPair(
        Dictionary<CardLibrary.SuitType, List<Card>> cardsBySuit,
        CardLibrary.SuitType[] suitPriority,
        List<Card> fallbackCards)
    {
        Card card1 = null;
        Card card2 = null;
        int bestSum = int.MaxValue;

        foreach (CardLibrary.SuitType suitType in suitPriority)
        {
            if (!cardsBySuit.ContainsKey(suitType) || cardsBySuit[suitType].Count < 2)
            {
                continue;
            }

            List<Card> suitCards = cardsBySuit[suitType];
            for (int i = 0; i < suitCards.Count - 1; i++)
            {
                for (int j = i + 1; j < suitCards.Count; j++)
                {
                    int sum = suitCards[i].NumericValue + suitCards[j].NumericValue;
                    if (sum >= 10 && sum < bestSum)
                    {
                        bestSum = sum;
                        card1 = suitCards[i];
                        card2 = suitCards[j];
                    }
                }
            }

            if (card1 != null)
            {
                break;
            }
        }

        return card1 != null ? (card1, card2) : (fallbackCards[0], fallbackCards[1]);
    }

    private void PlayCardPair(Card card1, Card card2)
    {
        GetTree().CreateTimer(.5).Timeout +=
            () => card1.GetNode<Button>("Button").EmitSignal(BaseButton.SignalName.Pressed);
        GetTree().CreateTimer(1.5).Timeout += () =>
        {
            card2.GetNode<Button>("Button").EmitSignal(BaseButton.SignalName.Pressed);
            EmitSignal(SignalName.EnemyFinishedTurn);
        };
    }

    #endregion
}
