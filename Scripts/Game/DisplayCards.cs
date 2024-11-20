using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class DisplayCards : Control
{
    private const float AllyPositionY = 340f;
    private const float EnemyPositionY = 200f;
    private readonly bool _shouldLog = true;

    private Node2D _cardHolder;
    private Node _cardsRoot;
    private bool _isAlly;

    public override void _Ready()
    {
        Logger.Debug("DisplayCards: Initializing display cards view", _shouldLog);
        InitializeCardHolder();
        CreateDisplayCards();
    }

    private void InitializeCardHolder()
    {
        _cardHolder = GetNode<Node2D>("CardHolder");
        SetCardHolderPosition();
    }

    private void SetCardHolderPosition()
    {
        float positionY = _isAlly ? AllyPositionY : EnemyPositionY;
        Logger.Debug($"DisplayCards: Setting card holder position Y to {positionY} for {(_isAlly ? "ally" : "enemy")}",
            _shouldLog);
        _cardHolder.Position = _cardHolder.Position with { Y = positionY };
    }

    private void CreateDisplayCards()
    {
        if (_cardsRoot == null)
        {
            Logger.Debug("DisplayCards: Warning: Cards root not set", _shouldLog);
            return;
        }

        List<Card> cards = _cardsRoot.GetChildren().OfType<Card>().ToList();
        Logger.Debug($"DisplayCards: Creating display cards for {cards.Count} cards", _shouldLog);

        foreach (Card card in cards)
        {
            CreateDisplayCard(card);
        }

        PositionDisplayCards();
    }

    private void CreateDisplayCard(Card originalCard)
    {
        Logger.Debug($"DisplayCards: Creating display card for {originalCard.GetCardName()}", _shouldLog);
        Card displayCard = originalCard.DuplicateCard(Card.CardMode.Display);
        _cardHolder.AddChild(displayCard);
    }

    private void PositionDisplayCards()
    {
        Logger.Debug("DisplayCards: Positioning display cards", _shouldLog);
        GameUtils.PositionCards(_cardHolder);
    }

    public void SetCardsRoot(Node cardsRoot)
    {
        Logger.Debug($"DisplayCards: Setting cards root: {cardsRoot.Name}", _shouldLog);
        _cardsRoot = cardsRoot;
    }

    public void SetIsAlly(bool isAlly)
    {
        Logger.Debug($"DisplayCards: Setting display type to {(isAlly ? "ally" : "enemy")}", _shouldLog);
        _isAlly = isAlly;
    }

    private void _OnPanelContainerGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.ButtonIndex == MouseButton.Left &&
            mouseEvent.Pressed)
        {
            Logger.Debug("DisplayCards: Display cards clicked, closing view", _shouldLog);
            QueueFree();
        }
    }

    public override void _ExitTree()
    {
        Logger.Debug("DisplayCards: Cleaning up display cards", _shouldLog);
        // Resources will be automatically cleaned up by Godot
        base._ExitTree();
    }
}
