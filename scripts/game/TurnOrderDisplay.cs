using System.Collections.Generic;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class TurnOrderDisplay : Control
{
    private readonly Dictionary<string, Texture2D> _avatarCache = new();
    private HBoxContainer _hBox;

    public override void _Ready()
    {
        _hBox = GetNode<HBoxContainer>("HBoxContainer");
    }

    public void AddAvatar(Player player)
    {
        TextureRect avatarContainer = new()
        {
            ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = GetAvatarPathFromSprite(player)
        };

        _hBox.AddChild(avatarContainer);
    }

    private Texture2D GetAvatarPathFromSprite(Player player)
    {
        // Logger.Debug($"Getting avatar path from sprite: {player.PlayerName}");
        // Get the sprite path from the player
        Sprite2D sprite = player.GetNode<Sprite2D>("Sprite2D");
        string spritePath = sprite.Texture.ResourcePath;

        // Convert sprite path to avatar path
        string avatarPath = spritePath.Replace("sprite", "avatar");

        // Load the texture using ResourceManager
        return ResourceManager.Load<Texture2D>(avatarPath);
    }

    public void CycleAvatars(Player player)
    {
        if (_hBox.GetChildCount() > 0)
        {
            // Get the first child
            Control firstAvatar = (Control)_hBox.GetChild(0);

            // Create a tween for fade out
            Tween tween = GetTree().CreateTween();
            tween.TweenProperty(firstAvatar, "modulate:a", 0.0f, 0.2f);

            // Remove and free the node when animation completes
            tween.TweenCallback(Callable.From(() =>
            {
                _hBox.RemoveChild(firstAvatar);
                firstAvatar.QueueFree();

                // Add new avatar with fade in
                TextureRect newAvatar = new()
                {
                    ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    Texture = GetAvatarPathFromSprite(player),
                    Modulate = new Color(1, 1, 1, 0) // Start fully transparent
                };

                _hBox.AddChild(newAvatar);

                // Create fade in tween
                Tween fadeInTween = GetTree().CreateTween();
                fadeInTween.TweenProperty(newAvatar, "modulate:a", 1.0f, 0.2f);
            }));
        }
        else
        {
            AddAvatar(player);
        }
    }
}
