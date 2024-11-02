using System;
using System.Collections.Generic;
using Godot;

namespace NinthLife.scripts.game
{
    public partial class Player : Node2D
    {
        [Signal]
        public delegate void PlayerFinishedDrawingEventHandler();

        private const int HandSize = 9;
        private const float AnimationSpeed = .25f;
        private StringName _combatEntitiesAdded;
        private int _currentCardPlays;
        private Hand _hand;
        private int _totalCardPlays = 2;

        public List<Card> Deck { get; } = new();

        public BaseBoard PlayerBoard { get; set; }
        public List<Card> Discard { get; } = new();
        public Polygon2D TurnIndicator { get; set; }
        public Vector2 BoardActivePosition { get; set; }
        public Vector2 BoardHiddenPosition { get; set; }
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

            PlayerBoard.GetNode<Label>("Label").Text = $"{PlayerName}'s Board";

            DrawCards(HandSize, .01);
        }

        public void StartTurn()
        {
            TurnIndicator.Visible = true;
            int amountToDraw = HandSize - _hand.GetChildren().Count;
            DrawCards(amountToDraw, .5);

            GetTree().CreateTimer(amountToDraw * .6).Timeout += () => _hand.EnableCards();
        }

        public void EndTurn()
        {
            TurnIndicator.Visible = false;
        }

        public void SlideHandIn()
        {
            Tween handTween = GetTree().CreateTween();
            _ = handTween.TweenProperty(
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
            _hand.DisableCards();
            _hand.CollapseHand();
            _ = handTween.TweenProperty(
                _hand,
                "global_position:y",
                GlobalPosition.Y + 750,
                AnimationSpeed
            );
        }

        public void SlideBoardIn()
        {
            Tween boardTween = GetTree().CreateTween();
            _ = boardTween.TweenProperty(
                PlayerBoard,
                "global_position:y",
                GlobalPosition.Y + BoardActivePosition.Y,
                AnimationSpeed
            );
        }

        public void SlideBoardOut()
        {
            Tween boardTween = GetTree().CreateTween();
            _ = boardTween.TweenProperty(
                PlayerBoard,
                "global_position:y",
                GlobalPosition.Y + BoardHiddenPosition.Y,
                AnimationSpeed
            );
        }

        public void SlideHandDisabled()
        {
            Tween handTween = GetTree().CreateTween();
            _ = handTween.TweenProperty(
                _hand,
                "global_position:y",
                GlobalPosition.Y + 575,
                AnimationSpeed
            );
            handTween.Finished += delegate
            {
                _hand.DisableCards();
                _hand.PositionCards();
            };
        }

        public void ShowBoard()
        {
            Tween boardTween = GetTree().CreateTween();
            _ = boardTween.TweenProperty(
                PlayerBoard,
                "global_position:y",
                GlobalPosition.Y + 650,
                AnimationSpeed
            );
        }

        public void EnablePlayerHand()
        {
            PlayerFinishedDrawing += _hand.EnableCards;
        }

        public async void DrawCards(int x, double delay = AnimationSpeed)
        {
            for (int i = 0; i < x; i++)
            {
                _ = await ToSignal(GetTree().CreateTimer(delay), "timeout");
                DrawCard();
                _hand.PositionCards();
            }
            _ = EmitSignal(SignalName.PlayerFinishedDrawing);
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
