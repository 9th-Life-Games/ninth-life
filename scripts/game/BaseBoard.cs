using System.Collections.Generic;
using Godot;

namespace NinthLife.scripts.game
{
    // Interface defining core board functionality
    public interface ICardBoard
    {
        List<Card> WeaponCards { get; }
        List<Card> ArmorCards { get; }
        List<Card> CoreCards { get; }
        List<Card> TalentCards { get; }
        List<Card> InPlayCards { get; }
        List<Card> DiscardCards { get; }

        void AddCard(Card card, CardPileType pileType);
        void RemoveCard(Card card, CardPileType pileType);
    }

    public enum CardPileType
    {
        Weapon,
        Armor,
        Core,
        Talent,
        InPlay,
        Discard,
    }

    // Base board implementation with common functionality
    public partial class BaseBoard : Node2D, ICardBoard
    {
        private readonly Dictionary<CardPileType, List<Card>> _cardPiles;

        public BaseBoard()
        {
            _cardPiles = new Dictionary<CardPileType, List<Card>>
            {
                { CardPileType.Weapon, new List<Card>() },
                { CardPileType.Armor, new List<Card>() },
                { CardPileType.Core, new List<Card>() },
                { CardPileType.Talent, new List<Card>() },
                { CardPileType.InPlay, new List<Card>() },
                { CardPileType.Discard, new List<Card>() },
            };
        }

        public List<Card> WeaponCards => _cardPiles[CardPileType.Weapon];
        public List<Card> ArmorCards => _cardPiles[CardPileType.Armor];
        public List<Card> CoreCards => _cardPiles[CardPileType.Core];
        public List<Card> TalentCards => _cardPiles[CardPileType.Talent];
        public List<Card> InPlayCards => _cardPiles[CardPileType.InPlay];
        public List<Card> DiscardCards => _cardPiles[CardPileType.Discard];

        public virtual void AddCard(Card card, CardPileType pileType)
        {
            _cardPiles[pileType].Add(card);
        }

        public virtual void RemoveCard(Card card, CardPileType pileType)
        {
            _ = _cardPiles[pileType].Remove(card);
        }
    }
}
