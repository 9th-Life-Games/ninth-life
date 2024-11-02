using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.game;

namespace NinthLife.scripts.utils
{
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
                combatEntity.BoardHiddenPosition = new(480, 575);
                combatEntity.BoardActivePosition = new(480, 272);
                combatEntity.PlayerBoard.Position = combatEntity.BoardHiddenPosition;
                combatEntity.AddChild(combatEntity.PlayerBoard, false, InternalMode.Front);
            }
            else
            {
                combatEntity.PlayerBoard = (BaseBoard)
                    GD.Load<PackedScene>("res://scenes/enemy_board.tscn").Instantiate();
                combatEntity.BoardHiddenPosition = new(576, -155);
                combatEntity.BoardActivePosition = new(576, 95);
                combatEntity.PlayerBoard.Position = combatEntity.BoardHiddenPosition;
                combatEntity.AddChild(combatEntity.PlayerBoard, false, InternalMode.Front);
            }

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

        public static Card DuplicateCard(Card oldCard)
        {
            PackedScene cardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
            Card card = (Card)cardScene.Instantiate();
            card.Texture = oldCard.Texture;
            card.NumericValue = oldCard.NumericValue;
            card.SuitType = oldCard.SuitType;
            card.IsFaceCard = oldCard.IsFaceCard;
            return card;
        }

        public override void _ExitTree()
        {
            foreach (Card card in CombatEntities.SelectMany(static player => player.Deck))
            {
                card.QueueFree();
            }

            foreach (Player player in CombatEntities)
            {
                player.QueueFree();
            }
        }
    }
}
