#nullable enable
using Godot;
using NinthLife.scripts.game;

namespace NinthLife.scripts.utils;

public static class Extensions
{
    public static Maybe<T> ToMaybe<T>(this T? value)
        where T : class
    {
        return value != null ? Maybe<T>.Some(value) : Maybe<T>.None();
    }


    public static Card DuplicateCard(this Card card, Card.CardMode mode = Card.CardMode.Disabled,
        bool playEnterAnimation = false)
    {
        PackedScene cardScene = ResourceManager.Load<PackedScene>("res://scenes/card.tscn");
        Card newCard = (Card)cardScene.Instantiate();
        newCard.Texture = card.Texture;
        newCard.Initialize(card.SuitType, card.SuitName, card.NumericValue, card.IsFaceCard);

        newCard.SetMode(mode, playEnterAnimation);

        return newCard;
    }
}
