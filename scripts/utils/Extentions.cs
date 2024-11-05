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


    public static Card DuplicateCard(this Card card, bool isClickable = true, bool initTreeEntered = false)
    {
        PackedScene cardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
        Card newCard = (Card)cardScene.Instantiate();
        newCard.Texture = card.Texture;
        newCard.NumericValue = card.NumericValue;
        newCard.CardName = card.CardName;
        newCard.SuitType = card.SuitType;
        newCard.IsFaceCard = card.IsFaceCard;
        newCard.Visible = true;
        newCard.GetNode<Button>("Button").Visible = isClickable;

        if (initTreeEntered)
        {
            Logger.Debug($"Card {newCard.NumericValue} init tree entered");
            newCard._Ready();
        }

        return newCard;
    }
}
