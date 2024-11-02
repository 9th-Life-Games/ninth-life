using System.Collections.Generic;
using System.Linq;
using Godot;

namespace NinthLife.scripts.game
{
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
        }

        private void OnChildExitedTree(Card _)
        {
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
            int spaceBetween = 0;

            if (GetTree() == null)
            {
                return;
            }

            List<Node> childList = GetChildren().ToList();

            Tween tween = GetTree().CreateTween().SetParallel();

            if (childList.Count <= 1)
            {
                foreach (Node child in childList)
                {
                    _ = tween.TweenProperty(child, "rotation_degrees", 0, AnimationSpeed);
                    _ = tween.TweenProperty(child, "position:x", 0, AnimationSpeed);
                    _ = tween.TweenProperty(child, "position:y", 5, AnimationSpeed);
                }
            }
            else
            {
                int spriteCount = childList.Count;
                int maxHeight = 5;

                int totalWidth = childList.Sum(card =>
                    ((Card)card).Texture.GetWidth() + spaceBetween
                );

                if (totalWidth > MaxScreenWidth)
                {
                    int overlap = totalWidth - MaxScreenWidth;
                    spaceBetween = overlap / (spriteCount - 1) * -1;
                    totalWidth = MaxScreenWidth;
                }

                int xPosition = -totalWidth / 2;
                int spriteCountStep = Mathf.RoundToInt(spriteCount / 3.0f);
                maxHeight += spriteCountStep * (5 + spriteCountStep);
                float angleStep = 2.0f * MaxAngle / (spriteCount - 1 > 1 ? spriteCount - 1 : 1);
                float currentAngle = -MaxAngle;
                int index = 0;

                foreach (Node card in childList)
                {
                    _ = tween.TweenProperty(card, "rotation_degrees", currentAngle, AnimationSpeed);

                    float relativeIndex = (float)index / (spriteCount - 1);
                    float arch = maxHeight * (4 * relativeIndex * (1 - relativeIndex));

                    _ = tween.TweenProperty(
                        card,
                        "position:x",
                        xPosition + (((Card)card).Texture.GetWidth() / 2f),
                        AnimationSpeed
                    );
                    _ = tween.TweenProperty(card, "position:y", -arch, AnimationSpeed);

                    xPosition += ((Card)card).Texture.GetWidth() + spaceBetween;
                    currentAngle += angleStep;

                    index++;
                }
            }
        }

        public void CollapseHand()
        {
            Tween tween = GetTree().CreateTween().SetParallel();

            foreach (Node card in GetChildren())
            {
                _ = tween.TweenProperty(card, "rotation_degrees", 0, AnimationSpeed);
                _ = tween.TweenProperty(
                    card,
                    "global_position:x",
                    GlobalPosition.X,
                    AnimationSpeed
                );
                _ = tween.TweenProperty(
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
    }
}
