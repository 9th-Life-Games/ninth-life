using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.components;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class Player : Node2D
{
    // Signals
    [Signal]
    public delegate void AllyHandEnabledEventHandler(bool enabled);

    [Signal]
    public delegate void AllyTurnReadyEventHandler();

    [Signal]
    public delegate void AttackCompleteEventHandler();

    [Signal]
    public delegate void DeathAnimationFinishedEventHandler();

    [Signal]
    public delegate void EnemyFinishedPlayingHandEventHandler();

    [Signal]
    public delegate void PlayerClickedEventHandler(Player player);

    [Signal]
    public delegate void PlayerFinishedDrawingEventHandler();

    [Signal]
    public delegate void PlayerMouseHoveredInEventHandler(Player player);

    // Constants
    private const int HandSize = 4;
    public const float AnimationSpeed = 0.25f;
    private const float DefenseDrawDelay = 1.75f;
    private const float EnemyPlayDelay1 = 0.5f;
    private const float EnemyPlayDelay2 = 1.5f;
    private const float HandDisabledY = 555f;
    private const float HandInY = 500f;
    private const float HandOutY = 850f;
    private const float BoardShowY = 650f;
    private readonly bool _shouldLog = true;

    // Node references
    private AnimationPlayer _animationPlayer;
    private Button _attackButton;
    private StringName _combatEntitiesAdded;
    private int _currentHealth;
    private Node2D _defenseDraw;
    private Button _endTurnButton;
    private Hand _hand;
    private HealthBar _healthBar;

    // State
    private int _initiativeBonus;
    private int _maxHealth;
    private Button _playerButton;
    public Health Health { get; private set; }
    public string WeaponType { get; private set; }

    // Public properties
    public int CurrentCardPlays { get; private set; }
    public int TotalCardPlays { get; private set; } = 2;
    public int MasteryBonus { get; private set; } = 3;
    public bool IsHandEnabled { get; private set; }
    public bool IsAlly { get; set; }
    public bool IsDead { get; private set; }
    public int Initiative { get; private set; }
    public string PlayerName { get; set; }

    // Visual indicators
    public Polygon2D PreviewIndicator { get; private set; }
    public Polygon2D AttackModeIndicator { get; private set; }
    public Polygon2D TurnIndicator { get; private set; }

    // Board related
    public BaseBoard PlayerBoard { get; set; }
    public Vector2 BoardActivePosition { get; set; }
    public Vector2 BoardHiddenPosition { get; set; }
    public List<Card> Deck { get; } = new();

    public override void _Ready()
    {
        Logger.Debug($"Player: Initializing player: {PlayerName}", _shouldLog);
        InitializeComponents();
        SetupEventHandlers();
        ConfigurePlayerBoard();
        InitializePlayerState();
        InitializeHealth();
    }

    public void PlayDeathAnimation()
    {
        Logger.Debug($"Player: Playing death animation for {PlayerName}", _shouldLog);
        _animationPlayer.Play("die");
    }

    public List<Card> GetActiveCards()
    {
        List<Card> activeCards = new();
        // Add any cards in play, in hand, etc.
        return activeCards;
    }

    private void InitializeComponents()
    {
        Logger.Debug("Player: Setting up player components", _shouldLog);
        _hand = GetNode<Hand>("Hand");
        _defenseDraw = GetNode<Node2D>("../../DefenseDraw");
        _attackButton = GetNode<Button>("../../Attack");
        _endTurnButton = GetNode<Button>("../../EndTurn");
        _playerButton = GetNode<Button>("Sprite2D/Button");

        _hand.SetPlayerBoard(PlayerBoard);
        SetIndicators();
    }

    private void SetupEventHandlers()
    {
        Logger.Debug("Player: Setting up event handlers", _shouldLog);
        _hand.CardPlayed += OnCardPlayed;
        _playerButton.Pressed += OnPlayerButtonPressed;
        _playerButton.MouseEntered += OnPlayerButtonMouseEntered;
        PlayerFinishedDrawing += EnablePlayerHand;

        SetupIdleAnimation();
    }

    private void ConfigurePlayerBoard()
    {
        if (PlayerBoard != null)
        {
            Logger.Debug($"Player: Configuring board for {PlayerName}", _shouldLog);
            PlayerBoard.GetNode<Label>("Label").Text = $"{PlayerName}'s Board";
        }
    }

    private void InitializePlayerState()
    {
        Logger.Debug($"Player: Initializing state for {PlayerName}", _shouldLog);
        DrawCards(HandSize, 0.01);
    }

    private void InitializeHealth()
    {
        Health = GetNode<Health>("Health");
        _healthBar = GetNode<HealthBar>("HealthBar");

        // Different health values for different characters
        int maxHealth = PlayerName switch
        {
            "Skull" => 20,
            "Hope" => 20,
            "Drinker" => 20,
            "Goblin" => 10,
            "Goblin 2" => 10,
            "Goblin 3" => 10,
            _ => 15
        };

        Health.Initialize(maxHealth);
        _healthBar.Initialize(Health);
        Health.UnitDied += OnUnitDied;
    }

    private void OnUnitDied()
    {
        IsDead = true;
    }

    public void PlayerDeath(Action callback)
    {
        _animationPlayer.Play("die");
        GetTree().CreateTimer(.3).Timeout += () =>
        {
            EmitSignal(SignalName.DeathAnimationFinished);
            callback?.Invoke();
            // QueueFree();
        };
    }

    private void OnPlayerButtonPressed()
    {
        Logger.Debug($"Player: Player button pressed for {PlayerName}", _shouldLog);
        EmitSignal(SignalName.PlayerClicked, this);
    }

    private void OnPlayerButtonMouseEntered()
    {
        Logger.Debug($"Player: Mouse entered player button for {PlayerName}", _shouldLog);
        EmitSignal(SignalName.PlayerMouseHoveredIn, this);
    }

    private void SetupIdleAnimation()
    {
        Logger.Debug($"Player: Setting up idle animation for {PlayerName}", _shouldLog);
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _animationPlayer.Play("idle");
        _animationPlayer.AnimationFinished += OnAnimationFinished;
    }

    public void OnPlayerAnimationFinished(Action<StringName> callback)
    {
        void OnFinished(StringName animationName)
        {
            _animationPlayer.AnimationFinished -= OnFinished;
            callback?.Invoke(animationName);
        }

        _animationPlayer.AnimationFinished += OnFinished;
    }

    private void OnAnimationFinished(StringName animationName)
    {
        Logger.Debug($"Player: Animation finished for {PlayerName}: {animationName}", _shouldLog);
        switch (animationName)
        {
            default:
                _animationPlayer.Play("idle");
                break;
        }
    }

    public void PlayHitAnimation()
    {
        Logger.Debug($"Player: Playing hit animation for {PlayerName}", _shouldLog);
        _animationPlayer.Play("hit");
    }

    public void PlayBlockAnimation()
    {
        Logger.Debug($"Player: Playing block animation for {PlayerName}", _shouldLog);
        _animationPlayer.Play("block");
    }

    public Card DrawCardForDefense(Action callback)
    {
        Logger.Debug($"Player: {PlayerName} drawing card for defense", _shouldLog);
        Card drawnCard = DrawAndSetupDefenseCard();
        ScheduleDefenseCardExit(drawnCard, callback);
        return drawnCard;
    }

    private Card DrawAndSetupDefenseCard()
    {
        Card drawnCard = Deck[0];
        Deck.RemoveAt(0);
        _defenseDraw.AddChild(drawnCard);
        drawnCard.SetMode(Card.CardMode.Disabled, true);
        return drawnCard;
    }

    private void ScheduleDefenseCardExit(Card card, Action callback)
    {
        GetTree().CreateTimer(DefenseDrawDelay).Timeout += () =>
        {
            card.ExitCard();
            callback.Invoke();
        };
    }

    private void OnCardPlayed()
    {
        CurrentCardPlays++;
        Logger.Debug($"Player: {PlayerName} played card {CurrentCardPlays}/{TotalCardPlays}", _shouldLog);

        if (CurrentCardPlays == TotalCardPlays && IsAlly)
        {
            Logger.Debug($"Player: Max plays reached for {PlayerName}, disabling hand", _shouldLog);
            SlideHandDisabled(false);
        }
    }

    public void SetButtonEnabled(bool enabled)
    {
        if (_playerButton != null)
        {
            Logger.Debug($"Player: Setting {PlayerName}'s button enabled: {enabled}", _shouldLog);
            _playerButton.Disabled = !enabled;
        }
    }

    public void StartTurn()
    {
        Logger.Debug($"Player: Starting turn for {PlayerName}", _shouldLog);
        InitializeTurn();
        DrawInitialCards();
        ScheduleTurnStart();
    }

    private void InitializeTurn()
    {
        DisablePlayerHand();
        TurnIndicator.Visible = true;
        CurrentCardPlays = 0;

        if (!IsAlly)
        {
            _attackButton.Disabled = true;
            _endTurnButton.Disabled = true;
        }
    }

    private void DrawInitialCards()
    {
        int amountToDraw = HandSize - _hand.GetChildren().Count;
        DrawCards(amountToDraw, 0.5);
    }

    private void ScheduleTurnStart()
    {
        int amountToDraw = HandSize - _hand.GetChildren().Count;
        GetTree().CreateTimer(amountToDraw * 0.6).Timeout += () =>
        {
            if (!IsAlly)
            {
                EnemyPlayHand();
            }
            else
            {
                EnableAllyTurn();
            }
        };
    }

    private void EnableAllyTurn()
    {
        Logger.Debug($"Player: Enabling ally turn for {PlayerName}", _shouldLog);
        _attackButton.Disabled = false;
        _attackButton.ButtonPressed = false;
        EnablePlayerHand();
        EmitSignal(SignalName.AllyTurnReady);
    }

    public void EndTurn()
    {
        Logger.Debug($"Player: Ending turn for {PlayerName}", _shouldLog);
        TurnIndicator.Visible = false;
    }

    private void SetIndicators()
    {
        Logger.Debug($"Player: Setting up indicators for {PlayerName}", _shouldLog);
        SetupTurnIndicator();
        SetupPreviewIndicator();
        SetupAttackModeIndicator();
    }

    private void SetupTurnIndicator()
    {
        TurnIndicator = GetNode<Polygon2D>("TurnIndicator");
        ConfigureIndicator(TurnIndicator);
    }

    private void SetupPreviewIndicator()
    {
        PreviewIndicator = GetNode<Polygon2D>("PreviewIndicator");
        ConfigureIndicator(PreviewIndicator);
    }

    private void SetupAttackModeIndicator()
    {
        AttackModeIndicator = GetNode<Polygon2D>("AttackModeIndicator");
        ConfigureIndicator(AttackModeIndicator);
    }

    private void ConfigureIndicator(Polygon2D indicator)
    {
        indicator.Polygon = new Vector2[] { new(-10, 0), new(10, 0), new(0, -20) };
        indicator.Rotate(Mathf.DegToRad(180));
    }

    public void ShowPreviewIndicator(bool show)
    {
        Logger.Debug($"Player: Setting preview indicator visibility for {PlayerName}: {show}", _shouldLog);
        PreviewIndicator.Visible = show;
    }

    public void TurnIndicatorPreviewColor(bool inPreviewMode)
    {
        Color color = !inPreviewMode ? new Color("00ff00") : new Color("006400");
        Logger.Debug($"Player: Setting turn indicator color for {PlayerName}: {color}", _shouldLog);
        TurnIndicator.Color = color;
    }

    public void SetAttackModeIndicator(bool inAttackMode)
    {
        Logger.Debug($"Player: Setting attack mode indicator for {PlayerName}: {inAttackMode}", _shouldLog);
        AttackModeIndicator.Visible = inAttackMode;
    }

    public void SetSelectedEnemy(bool selected)
    {
        Logger.Debug($"Player: Setting selected enemy state for {PlayerName}: {selected}", _shouldLog);
        AttackModeIndicator.Color = selected ? Colors.Orange : Colors.Yellow;
    }

    public void EnemyPlayHand()
    {
        Logger.Debug($"Player: Enemy {PlayerName} playing hand", _shouldLog);
        (Card card1, Card card2) cardsToPlay = SelectCardsToPlay();
        PlayEnemyCards(cardsToPlay.card1, cardsToPlay.card2);
    }

    private (Card card1, Card card2) SelectCardsToPlay()
    {
        List<Card> cardsInHand = _hand.GetChildren().OfType<Card>().ToList();
        List<Card> nonFaceCards = cardsInHand.Where(card => !card.IsFaceCard).ToList();
        (Card card1, Card card2) bestPair = FindBestCardPair(nonFaceCards);

        if (bestPair.card1 != null)
        {
            Logger.Debug(
                $"Player: Enemy found optimal play: {bestPair.card1.GetCardName()} + {bestPair.card2.GetCardName()}",
                _shouldLog);
            return bestPair;
        }

        Logger.Debug("Player: Enemy using fallback play with first two cards", _shouldLog);
        return (cardsInHand[0], cardsInHand[1]);
    }

    private (Card card1, Card card2) FindBestCardPair(List<Card> nonFaceCards)
    {
        Dictionary<CardLibrary.SuitType, List<Card>> cardsBySuit = nonFaceCards
            .GroupBy(card => card.SuitType)
            .ToDictionary(g => g.Key, g => g.ToList());

        CardLibrary.SuitType[] suitPriority =
        {
            CardLibrary.SuitType.Core, CardLibrary.SuitType.Talent, CardLibrary.SuitType.Weapon,
            CardLibrary.SuitType.Armor
        };

        return FindOptimalCardPair(cardsBySuit, suitPriority);
    }

    private (Card card1, Card card2) FindOptimalCardPair(
        Dictionary<CardLibrary.SuitType, List<Card>> cardsBySuit,
        CardLibrary.SuitType[] suitPriority)
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
            (bool found, Card card1, Card card2) pair = FindBestPairInSuit(suitCards, ref bestSum);
            if (pair.found)
            {
                card1 = pair.card1;
                card2 = pair.card2;
                break;
            }
        }

        return (card1, card2);
    }

    private (bool found, Card card1, Card card2) FindBestPairInSuit(List<Card> suitCards, ref int bestSum)
    {
        Card card1 = null;
        Card card2 = null;

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

        return (card1 != null, card1, card2);
    }

    private void PlayEnemyCards(Card card1, Card card2)
    {
        Logger.Debug($"Player: Enemy {PlayerName} scheduling card plays", _shouldLog);
        GetTree().CreateTimer(EnemyPlayDelay1).Timeout +=
            () => card1.GetNode<Button>("Button").EmitSignal(BaseButton.SignalName.Pressed);

        GetTree().CreateTimer(EnemyPlayDelay2).Timeout += () =>
        {
            card2.GetNode<Button>("Button").EmitSignal(BaseButton.SignalName.Pressed);
            EmitSignal(SignalName.EnemyFinishedPlayingHand);
        };
    }

    public void SlideHandIn()
    {
        Logger.Debug($"Player: Sliding in hand for {PlayerName}", _shouldLog);
        Tween handTween = GetTree().CreateTween();
        handTween.TweenProperty(
            _hand,
            "global_position:y",
            GlobalPosition.Y + HandInY,
            AnimationSpeed
        );
        handTween.Finished += () => _hand.PositionCards();
    }

    public void SlideHandOut()
    {
        Logger.Debug($"Player: Sliding out hand for {PlayerName}", _shouldLog);
        Tween handTween = GetTree().CreateTween();
        DisablePlayerHand();
        _hand.CollapseHand();
        handTween.TweenProperty(
            _hand,
            "global_position:y",
            GlobalPosition.Y + HandOutY,
            AnimationSpeed
        );
    }

    public void SlideBoardIn()
    {
        Logger.Debug($"Player: Sliding in board for {PlayerName}", _shouldLog);
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
        Logger.Debug($"Player: Sliding out board for {PlayerName}", _shouldLog);
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
        Logger.Debug($"Player: Sliding to disabled position for {PlayerName}", _shouldLog);
        Tween handTween = GetTree().CreateTween();
        handTween.TweenProperty(
            _hand,
            "global_position:y",
            GlobalPosition.Y + HandDisabledY,
            AnimationSpeed
        );
        handTween.Finished += () =>
        {
            DisablePlayerHand(emitSignal);
            _hand.PositionCards();
        };
    }

    public void EnablePlayerHand()
    {
        Logger.Debug($"Player: Enabling hand for {PlayerName}", _shouldLog);
        IsHandEnabled = true;
        _hand.EnableCards();
        EmitSignal(SignalName.AllyHandEnabled, true);
    }

    private void DisablePlayerHand(bool emitSignal = true)
    {
        Logger.Debug($"Player: Disabling hand for {PlayerName} (emit signal: {emitSignal})", _shouldLog);
        IsHandEnabled = false;
        _hand.DisableCards();
        if (emitSignal)
        {
            EmitSignal(SignalName.AllyHandEnabled, false);
        }
    }

    private async void DrawCards(int count, double delay = AnimationSpeed)
    {
        Logger.Debug($"Player: Drawing {count} cards for {PlayerName}", _shouldLog);
        for (int i = 0; i < count; i++)
        {
            await ToSignal(GetTree().CreateTimer(delay), "timeout");
            DrawCard();
            _hand.PositionCards();
        }

        EmitSignal(SignalName.PlayerFinishedDrawing);
    }

    private void DrawCard()
    {
        if (Deck.Count <= 0)
        {
            Logger.Debug($"Player: Cannot draw card - {PlayerName}'s deck is empty", _shouldLog);
            return;
        }

        Card drawnCard = Deck[0];
        Deck.RemoveAt(0);
        Logger.Debug($"Player: {PlayerName} drew card: {drawnCard.GetCardName()}", _shouldLog);
        AddCardToHand(drawnCard);
    }

    private void AddCardToHand(Card card)
    {
        Logger.Debug($"Player: Adding {card.GetCardName()} to {PlayerName}'s hand", _shouldLog);
        _hand.AddChild(card);
    }

    public void CalculateInitiative()
    {
        Random rand = new();
        // switch (PlayerName)
        // {
        //     case "Skull":
        //         _initiativeBonus += 100;
        //         break;
        // }

        Initiative = rand.Next(100) + 1 + _initiativeBonus;
        Logger.Debug(
            $"Player: Calculated initiative for {PlayerName}: {Initiative} (base: {Initiative - _initiativeBonus}, bonus: {_initiativeBonus})",
            _shouldLog);
    }

    public void SetWeaponType(string weaponType)
    {
        WeaponType = weaponType;
    }

    public void ShuffleDeck(int repeat)
    {
        Logger.Debug($"Player: Shuffling {PlayerName}'s deck {repeat} times", _shouldLog);
        for (int index = 0; index < repeat; index++)
        {
            PerformShuffle();
        }
    }

    private void PerformShuffle()
    {
        Random rng = new();
        for (int i = Deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (Deck[i], Deck[j]) = (Deck[j], Deck[i]);
        }
    }

    public override void _ExitTree()
    {
        Logger.Debug($"Player: Cleaning up resources for {PlayerName}", _shouldLog);
        CleanupDeck();
        CleanupAnimations();
    }

    private void CleanupDeck()
    {
        foreach (Card card in Deck)
        {
            card.Texture = null;
            card.QueueFree();
        }

        Deck.Clear();
    }

    private void CleanupAnimations()
    {
        if (_animationPlayer != null)
        {
            _animationPlayer.Stop();
        }
    }
}
