using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.game;

namespace NinthLife.scripts.utils;

public partial class GameUtils : Node
{
    public static readonly List<Player> CombatEntities = new();


    public static Player InstantiateCombatEntity(
        string name,
        Texture texture,
        Vector2 position,
        bool isAlly = false,
        int hFrames = 3
    )
    {
        Player combatEntity = (Player)
            GD.Load<PackedScene>("res://scenes/player.tscn").Instantiate();
        combatEntity.IsAlly = isAlly;
        combatEntity.PlayerName = name;

        if (isAlly)
        {
            combatEntity.PlayerBoard = (BaseBoard)
                GD.Load<PackedScene>("res://scenes/player_board.tscn").Instantiate();
            combatEntity.BoardHiddenPosition = new Vector2(480, 575);
            combatEntity.BoardActivePosition = new Vector2(480, 272);
        }
        else
        {
            combatEntity.PlayerBoard = (BaseBoard)
                GD.Load<PackedScene>("res://scenes/enemy_board.tscn").Instantiate();
            combatEntity.BoardHiddenPosition = new Vector2(576, -155);
            combatEntity.BoardActivePosition = new Vector2(576, 95);
        }

        combatEntity.PlayerBoard.Position = combatEntity.BoardHiddenPosition;
        combatEntity.AddChild(combatEntity.PlayerBoard, false, InternalMode.Front);

        Sprite2D sprite = combatEntity.GetNode<Sprite2D>("Sprite2D");
        sprite.Texture = (Texture2D)texture;
        sprite.Hframes = hFrames;
        sprite.Position = position;

        Polygon2D turnIndicator = combatEntity.GetNode<Polygon2D>("TurnIndicator");
        float spriteHeight = sprite.Texture.GetHeight();
        turnIndicator.Position = new Vector2(position.X, position.Y + (-spriteHeight / 2) - 20);
        turnIndicator.Color = isAlly ? new Color("00ff00") : new Color("ff0000");

        return combatEntity;
    }

    public override void _ExitTree()
    {
        foreach (Player player in CombatEntities)
        {
            foreach (Card card in player.Deck)
            {
                card.Texture = null; // Clear texture reference
                card.QueueFree();
            }

            player.QueueFree();
        }

        CombatEntities.Clear();
    }

    public static void PositionCards(Node2D container, bool curved = true, float animationSpeed = 0.1f)
    {
        const int maxScreenWidth = 900;
        const int maxAngle = 10;
        int spaceBetween = 0;

        if (container.GetTree() == null)
        {
            return;
        }

        List<Card> childList = container.GetChildren().OfType<Card>().ToList();

        // If no cards, don't create a tween at all
        if (childList.Count == 0)
        {
            return;
        }

        Tween tween = container.GetTree().CreateTween().SetParallel();

        if (childList.Count <= 1)
        {
            foreach (Card child in childList)
            {
                tween.TweenProperty(child, "rotation_degrees", 0, animationSpeed);
                tween.TweenProperty(child, "position:x", 0, animationSpeed);
                tween.TweenProperty(child, "position:y", 5, animationSpeed);
            }

            return;
        }

        int spriteCount = childList.Count;
        int maxHeight = curved ? 5 : 0; // Only apply height if curved is true

        int totalWidth = childList.Sum(card =>
            card.Texture.GetWidth() + spaceBetween
        );

        if (totalWidth > maxScreenWidth)
        {
            int overlap = totalWidth - maxScreenWidth;
            spaceBetween = overlap / (spriteCount - 1) * -1;
            totalWidth = maxScreenWidth;
        }

        int xPosition = -totalWidth / 2;
        int spriteCountStep = Mathf.RoundToInt(spriteCount / 3.0f);
        maxHeight += curved ? spriteCountStep * (5 + spriteCountStep) : 0;
        float angleStep = 2.0f * maxAngle / (spriteCount - 1 > 1 ? spriteCount - 1 : 1);
        float currentAngle = -maxAngle;
        int index = 0;

        foreach (Card card in childList)
        {
            // Only apply rotation if curved is true
            if (curved)
            {
                tween.TweenProperty(card, "rotation_degrees", currentAngle, animationSpeed);
            }
            else
            {
                tween.TweenProperty(card, "rotation_degrees", 0, animationSpeed);
            }

            float relativeIndex = (float)index / (spriteCount - 1);
            float arch = curved ? maxHeight * (4 * relativeIndex * (1 - relativeIndex)) : 0;

            tween.TweenProperty(
                card,
                "position:x",
                xPosition + (card.Texture.GetWidth() / 2f),
                animationSpeed
            );
            tween.TweenProperty(card, "position:y", -arch, animationSpeed);

            xPosition += card.Texture.GetWidth() + spaceBetween;
            currentAngle += angleStep;
            index++;
        }
    }
}
