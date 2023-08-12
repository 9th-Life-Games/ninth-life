using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.game;

namespace NinthLife.scripts.utils;

public partial class GameUtils : Node
{
    public static readonly List<Player> CombatEntities = new();

    public static Player InstantiateCombatEntity(string name, Texture texture,
        Vector2 position,
        bool isAlly = false,
        int hFrames = 3)
    {
        var combatEntity = (Player)GD.Load<PackedScene>("res://scenes/player.tscn").Instantiate();
        combatEntity.IsAlly = isAlly;
        combatEntity.PlayerName = name;
        var sprite = combatEntity.GetNode<Sprite2D>("Sprite2D");
        sprite.Texture = (Texture2D)texture;
        sprite.Hframes = hFrames;
        sprite.Position = position;

        return combatEntity;
    }

    public static Card DuplicateCard(Card oldCard)
    {
        var cardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
        var card = (Card)cardScene.Instantiate();
        card.Texture = oldCard.Texture;
        card.NumericValue = oldCard.NumericValue;
        card.SuitType = oldCard.SuitType;
        card.IsFaceCard = oldCard.IsFaceCard;
        return card;
    }

    public override void _ExitTree()
    {
        foreach (var card in CombatEntities.SelectMany(player => player.Deck))
            card.QueueFree();
        foreach (var player in CombatEntities)
            player.QueueFree();
    }
}