using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class DisplayCards : Control
{
    private Node2D _cardHolder;
    private Node _cardsRoot;
    private bool _isAlly;

    public override void _Ready()
    {
        _cardHolder = GetNode<Node2D>("CardHolder");
        if (_isAlly)
        {
            _cardHolder.Position = _cardHolder.Position with { Y = 340 };
        }
        else
        {
            _cardHolder.Position = _cardHolder.Position with { Y = 200 };
        }

        foreach (Card card in _cardsRoot.GetChildren().OfType<Card>())
        {
            Logger.Debug($"Card: {card.NumericValue}");
            Card newCard = card.DuplicateCard(Card.CardMode.Display);
            _cardHolder.AddChild(newCard);
        }

        GameUtils.PositionCards(_cardHolder);
    }

    public void SetCardsRoot(Node cardsRoot)
    {
        _cardsRoot = cardsRoot;
    }

    public void SetIsAlly(bool isAlly)
    {
        _isAlly = isAlly;
    }

    private void _OnPanelContainerGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            QueueFree();
        }
    }
}
