using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.game;

namespace NinthLife.scripts.utils
{
    public partial class CardLibrary : Node
    {
        public enum SuitType
        {
            Weapon,
            Armor,
            Core,
            Talent,
        }

        public static readonly Dictionary<string, CardData> Suits =
            new()
            {
                {
                    "heavyarmor",
                    new CardData
                    {
                        Type = "armor",
                        SubType = null,
                        Cards = new List<Card>(),
                    }
                },
                {
                    "lightarmor",
                    new CardData
                    {
                        Type = "armor",
                        SubType = null,
                        Cards = new List<Card>(),
                    }
                },
                {
                    "longblade",
                    new CardData
                    {
                        Type = "weapon",
                        SubType = null,
                        Cards = new List<Card>(),
                    }
                },
                {
                    "shortblade",
                    new CardData
                    {
                        Type = "weapon",
                        SubType = null,
                        Cards = new List<Card>(),
                    }
                },
                {
                    "mace",
                    new CardData
                    {
                        Type = "weapon",
                        SubType = null,
                        Cards = new List<Card>(),
                    }
                },
                {
                    "suns",
                    new CardData
                    {
                        Type = "priest",
                        SubType = "core",
                        Cards = new List<Card>(),
                    }
                },
                {
                    "cures",
                    new CardData
                    {
                        Type = "priest",
                        SubType = "talent",
                        Cards = new List<Card>(),
                    }
                },
                {
                    "shadows",
                    new CardData
                    {
                        Type = "thief",
                        SubType = "core",
                        Cards = new List<Card>(),
                    }
                },
                {
                    "venom",
                    new CardData
                    {
                        Type = "thief",
                        SubType = "talent",
                        Cards = new List<Card>(),
                    }
                },
                {
                    "oracles",
                    new CardData
                    {
                        Type = "chronomancer",
                        SubType = "core",
                        Cards = new List<Card>(),
                    }
                },
                {
                    "paths",
                    new CardData
                    {
                        Type = "chronomancer",
                        SubType = "talent",
                        Cards = new List<Card>(),
                    }
                },
                {
                    "lions",
                    new CardData
                    {
                        Type = "warrior",
                        SubType = "core",
                        Cards = new List<Card>(),
                    }
                },
                {
                    "standards",
                    new CardData
                    {
                        Type = "warrior",
                        SubType = "talent",
                        Cards = new List<Card>(),
                    }
                },
                {
                    "wicks",
                    new CardData
                    {
                        Type = "flametongue",
                        SubType = "core",
                        Cards = new List<Card>(),
                    }
                },
                {
                    "wax",
                    new CardData
                    {
                        Type = "flametongue",
                        SubType = "talent",
                        Cards = new List<Card>(),
                    }
                },
            };

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            InitializeCards();
        }

        public override void _ExitTree()
        {
            foreach (Card card in Suits.SelectMany(static suitData => suitData.Value.Cards))
            {
                card.QueueFree();
            }
        }

        private static void InitializeCards()
        {
            foreach ((string key, CardData suit) in Suits)
            {
                for (int number = 1; number <= 13; number++)
                {
                    string cardImg = $"res://assets/Card Art/{suit.Type}/{key}/{number}_{key}.png";
                    PackedScene cardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
                    Card card = (Card)cardScene.Instantiate();
                    card.Name = key;

                    card.SuitType =
                        suit.SubType != null
                            ? suit.SubType switch
                            {
                                "core" => SuitType.Core,
                                "talent" => SuitType.Talent,
                                _ => card.SuitType,
                            }
                            : suit.Type switch
                            {
                                "armor" => SuitType.Armor,
                                "weapon" => SuitType.Weapon,
                                _ => card.SuitType,
                            };

                    switch (number)
                    {
                        case 1:
                            cardImg = cardImg.Replace("1", "ace");
                            card.NumericValue = 23;
                            card.IsFaceCard = true;
                            break;
                        case 11:
                            cardImg = cardImg.Replace("11", "jack");
                            card.NumericValue = 20;
                            card.IsFaceCard = true;
                            break;
                        case 12:
                            cardImg = cardImg.Replace("12", "queen");
                            card.NumericValue = 21;
                            card.IsFaceCard = true;
                            break;
                        case 13:
                            cardImg = cardImg.Replace("13", "king");
                            card.NumericValue = 22;
                            card.IsFaceCard = true;
                            break;
                        default:
                            card.NumericValue = number;
                            break;
                    }

                    card.Texture = GD.Load<Texture2D>(cardImg);
                    card.GetNode<Button>("Button").Visible = false;
                    suit.Cards.Add(card);
                }
            }
        }

        public class CardData
        {
            public List<Card> Cards { get; set; }
            public string SubType { get; set; }
            public string Type { get; set; }
        }
    }
}
