using System;
using System.Collections.Generic;
using Godot;

namespace NinthLife.scripts.game
{
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
        public Polygon2D TurnIndicator { get; set; }
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

            TurnIndicator = GetNode<Polygon2D>("TurnIndicator");
            TurnIndicator.Polygon = new Vector2[] { new(-10, 0), new(10, 0), new(0, -20) };
            TurnIndicator.Rotate(Mathf.DegToRad(180));

            DrawCards(HandSize, .01);
        }

        public void StartTurn()
        {
            GD.Print($"{PlayerName} turn started");
            TurnIndicator.Visible = true;
            int amountToDraw = HandSize - _hand.GetChildren().Count;
            DrawCards(amountToDraw, .5);

            GetTree().CreateTimer(amountToDraw * .6).Timeout += () => _hand.EnableCards();
            _ = EmitSignal(SignalName.PlayerTurnStarted);
        }

        public void EndTurn()
        {
            GD.Print($"{PlayerName} turn ended");
            TurnIndicator.Visible = false;
            _ = EmitSignal(SignalName.PlayerTurnEnded);
        }

        public void SlideHandIn()
        {
            GD.Print($"{PlayerName}'s hand sliding IN");
            Tween tween = GetTree().CreateTween();
            _ = tween.TweenProperty(
                _hand,
                "global_position:y",
                GlobalPosition.Y + 500,
                AnimationSpeed
            );
            tween.Finished += delegate
            {
                _hand.PositionCards();
            };
        }

        public void SlideHandOut()
        {
            GD.Print($"{PlayerName}'s hand sliding OUT");
            Tween tween = GetTree().CreateTween();
            _hand.DisableCards();
            _hand.CollapseHand();
            _ = tween.TweenProperty(
                _hand,
                "global_position:y",
                GlobalPosition.Y + 750,
                AnimationSpeed
            );
        }

        public void SlideHandDisabled()
        {
            GD.Print($"{PlayerName}'s hand DISABLED");
            Tween tween = GetTree().CreateTween();
            _ = tween.TweenProperty(
                _hand,
                "global_position:y",
                GlobalPosition.Y + 575,
                AnimationSpeed
            );
            tween.Finished += delegate
            {
                _hand.DisableCards();
                _hand.PositionCards();
            };
        }

        public async void DrawCards(int x, double delay = AnimationSpeed)
        {
            for (int i = 0; i < x; i++)
            {
                _ = await ToSignal(GetTree().CreateTimer(delay), "timeout");
                DrawCard();
                _hand.PositionCards();
            }
            GD.Print("Finished Drawing Cards");
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta) { }

        // Add a card to the player's deck
        public void AddCardToDeck(Card card)
        {
            Deck.Add(card);
        }

        public void CalculateInitiative()
        {
            Random rand = new();
            Initiative = rand.Next(100) + 1 + InitiativeBonus;
        }

        // Draw a card from the deck
        public void DrawCard()
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
        public void AddCardToHand(Card card)
        {
            _hand.AddChild(card);
        }

        // Add a card to the player's discard
        public void AddCardToDiscard(Card card)
        {
            Discard.Add(card);
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
                card.QueueFree();
            }
        }
    }
}
