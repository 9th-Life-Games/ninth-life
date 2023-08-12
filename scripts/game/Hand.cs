using System.Linq;
using Godot;

namespace NinthLife.scripts.game;

public partial class Hand : Node2D
{
    private const float AnimationSpeed = .1f;
    private const int MaxScreenWidth = 900;

    private const int MaxAngle = 10;

    public override void _Ready()
    {
        ChildEnteredTree += OnChildEnteredTree;
        PositionCards();
    }

    private void OnChildEnteredTree(Node node)
    {
        ((Card)node).CardLeftTree += OnChildExitedTree;
        // PositionCards();
    }

    private void OnChildExitedTree(Card _)
    {
        GetTree().CreateTimer(.00000001).Timeout += PositionCards;
    }

    public void DisableCards()
    {
        foreach (var card in GetChildren().OfType<Card>()) card.DisableCard();
    }

    public void EnableCards()
    {
        foreach (var card in GetChildren().OfType<Card>()) card.EnableCard();
    }

    public void PositionCards()
    {
        var spaceBetween = 0;


        if (GetTree() == null) return;
        var childList = GetChildren().ToList();

        var tween = GetTree().CreateTween().SetParallel();

        if (childList.Count <= 1)
        {
            foreach (var child in childList)
            {
                tween.TweenProperty(child, "rotation_degrees", 0, AnimationSpeed);
                tween.TweenProperty(child, "position:x", 0, AnimationSpeed);
                tween.TweenProperty(child, "position:y", 5, AnimationSpeed);
            }
        }
        else
        {
            var spriteCount = childList.Count;
            var maxHeight = 5;

            var totalWidth = childList.Sum(card => ((Card)card).Texture.GetWidth() + spaceBetween);

            if (totalWidth > MaxScreenWidth)
            {
                var overlap = totalWidth - MaxScreenWidth;
                spaceBetween = overlap / (spriteCount - 1) * -1;
                totalWidth = MaxScreenWidth;
            }

            var xPosition = -totalWidth / 2;
            var spriteCountStep = Mathf.RoundToInt(spriteCount / 3.0f);
            maxHeight += spriteCountStep * (5 + spriteCountStep);
            var angleStep = 2.0f * MaxAngle / (spriteCount - 1 > 1 ? spriteCount - 1 : 1);
            float currentAngle = -MaxAngle;
            var index = 0;

            foreach (var card in childList)
            {
                tween.TweenProperty(card, "rotation_degrees", currentAngle, AnimationSpeed);

                var relativeIndex = (float)index / (spriteCount - 1);
                var arch = maxHeight * (4 * relativeIndex * (1 - relativeIndex));

                tween.TweenProperty(card,
                    "position:x",
                    xPosition + ((Card)card).Texture.GetWidth() / 2f,
                    AnimationSpeed);
                tween.TweenProperty(card, "position:y", -arch, AnimationSpeed);

                xPosition += ((Card)card).Texture.GetWidth() + spaceBetween;
                currentAngle += angleStep;

                index++;
            }
        }
    }

    public void CollapseHand()
    {
        var tween = GetTree().CreateTween().SetParallel();

        foreach (var card in GetChildren())
        {
            tween.TweenProperty(card, "rotation_degrees", 0, AnimationSpeed);
            tween.TweenProperty(card, "global_position:x", GlobalPosition.X, AnimationSpeed);
            tween.TweenProperty(card, "global_position:y", GlobalPosition.Y, AnimationSpeed);
        }
    }

    public override void _ExitTree()
    {
        foreach (var child in GetChildren())
            if (child is Card card)
                card.QueueFree();
    }
}