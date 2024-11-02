using System.Collections.Generic;
using Godot;
using NinthLife.scripts.game;

namespace NinthLife.scripts.utils
{
    public partial class Board : Node2D
    {
        public List<Card> WeaponCards { get; set; } = new List<Card>();
        public List<Card> ArmorCards { get; set; } = new List<Card>();
        public List<Card> CoreCards { get; set; } = new List<Card>();
        public List<Card> TalentCards { get; set; } = new List<Card>();
        public List<Card> InPlayCards { get; set; } = new List<Card>();
        public List<Card> DiscardCards { get; set; } = new List<Card>();

        public virtual Player Player { get; set; }

        // Common logic methods
        public static void AddCardToCategory(Card card, List<Card> category)
        {
            category.Add(card);
        }

        public static void RemoveCardFromCategory(Card card, List<Card> category)
        {
            _ = category.Remove(card);
        }

        // Placeholder methods for visual logic to be overridden
        public virtual void SlideIn(float animationSpeed) { }

        public virtual void SlideOut(float animationSpeed) { }
    }
}
