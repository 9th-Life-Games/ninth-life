using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class Hand : Node2D
{
    private const float AnimationSpeed = .1f;
    private const int MaxScreenWidth = 900;

    private const int MaxAngle = 10;

    private BaseBoard _playerBoard;

    public override void _Ready()
    {
        ChildEnteredTree += OnChildEnteredTree;
        PositionCards();
    }

    private void OnChildEnteredTree(Node node)
    {
        ((Card)node).CardLeftTree += OnChildExitedTree;
    }

    private void OnChildExitedTree(Card card)
    {
        _playerBoard.AddCard(card);
        GetTree().CreateTimer(.00000001).Timeout += PositionCards;
    }

    public void DisableCards()
    {
        foreach (Card card in GetChildren().OfType<Card>())
        {
            card.DisableCard();
        }
    }

    public void EnableCards()
    {
        foreach (Card card in GetChildren().OfType<Card>())
        {
            card.EnableCard();
        }
    }

    public void PositionCards()
    {
        GameUtils.PositionCards(this);
        // int spaceBetween = 0;
        //
        // if (GetTree() == null)
        // {
        //     return;
        // }
        //
        // List<Node> childList = GetChildren().ToList();
        //
        // Tween tween = GetTree().CreateTween().SetParallel();
        //
        // if (childList.Count <= 1)
        // {
        //     foreach (Node child in childList)
        //     {
        //         tween.TweenProperty(child, "rotation_degrees", 0, AnimationSpeed);
        //         tween.TweenProperty(child, "position:x", 0, AnimationSpeed);
        //         tween.TweenProperty(child, "position:y", 5, AnimationSpeed);
        //     }
        // }
        // else
        // {
        //     int spriteCount = childList.Count;
        //     int maxHeight = 5;
        //
        //     int totalWidth = childList.Sum(card =>
        //         ((Card)card).Texture.GetWidth() + spaceBetween
        //     );
        //
        //     if (totalWidth > MaxScreenWidth)
        //     {
        //         int overlap = totalWidth - MaxScreenWidth;
        //         spaceBetween = overlap / (spriteCount - 1) * -1;
        //         totalWidth = MaxScreenWidth;
        //     }
        //
        //     int xPosition = -totalWidth / 2;
        //     int spriteCountStep = Mathf.RoundToInt(spriteCount / 3.0f);
        //     maxHeight += spriteCountStep * (5 + spriteCountStep);
        //     float angleStep = 2.0f * MaxAngle / (spriteCount - 1 > 1 ? spriteCount - 1 : 1);
        //     float currentAngle = -MaxAngle;
        //     int index = 0;
        //
        //     foreach (Node card in childList)
        //     {
        //         tween.TweenProperty(card, "rotation_degrees", currentAngle, AnimationSpeed);
        //
        //         float relativeIndex = (float)index / (spriteCount - 1);
        //         float arch = maxHeight * (4 * relativeIndex * (1 - relativeIndex));
        //
        //         tween.TweenProperty(
        //             card,
        //             "position:x",
        //             xPosition + (((Card)card).Texture.GetWidth() / 2f),
        //             AnimationSpeed
        //         );
        //         tween.TweenProperty(card, "position:y", -arch, AnimationSpeed);
        //
        //         xPosition += ((Card)card).Texture.GetWidth() + spaceBetween;
        //         currentAngle += angleStep;
        //
        //         index++;
        //     }
        // }
    }

    public void CollapseHand()
    {
        Tween tween = GetTree().CreateTween().SetParallel();

        foreach (Node card in GetChildren())
        {
            tween.TweenProperty(card, "rotation_degrees", 0, AnimationSpeed);
            tween.TweenProperty(
                card,
                "global_position:x",
                GlobalPosition.X,
                AnimationSpeed
            );
            tween.TweenProperty(
                card,
                "global_position:y",
                GlobalPosition.Y,
                AnimationSpeed
            );
        }
    }

    public override void _ExitTree()
    {
        foreach (Node child in GetChildren())
        {
            if (child is Card card)
            {
                card.QueueFree();
            }
        }
    }

    public void SetPlayerBoard(BaseBoard baseBoard)
    {
        _playerBoard = baseBoard;
    }
}
