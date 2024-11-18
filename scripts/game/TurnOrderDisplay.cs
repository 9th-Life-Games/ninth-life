using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class TurnOrderDisplay : Control
{
    private const float FadeAnimationDuration = 0.2f;
    private const float DefaultAlpha = 1.0f;
    private const float TransparentAlpha = 0.0f;

    private readonly Dictionary<string, Texture2D> _avatarCache = new();
    private readonly Dictionary<Player, Control> _playerAvatars = new();
    private readonly bool _shouldLog = true;
    private HBoxContainer _hBox;

    public override void _Ready()
    {
        Logger.Debug("TurnOrderDisplay: Initializing turn order display", _shouldLog);
        InitializeContainer();
    }

    private void InitializeContainer()
    {
        _hBox = GetNode<HBoxContainer>("HBoxContainer");
    }

    public void AddAvatar(Player player)
    {
        Logger.Debug($"TurnOrderDisplay: Adding avatar for player: {player.PlayerName}", _shouldLog);
        Control avatarContainer = CreateAvatarContainer(player);
        _hBox.AddChild(avatarContainer);
        _playerAvatars[player] = avatarContainer;
    }

    private ColorRect CreateAvatarWrapper(Control avatarContainer)
    {
        ColorRect avatarContainerColorRect = new();
        avatarContainerColorRect.Color = Colors.Gray with { A = 1 };
        avatarContainerColorRect.AddChild(avatarContainer);
        avatarContainerColorRect.CustomMinimumSize = avatarContainerColorRect.CustomMinimumSize with { X = 50 };
        return avatarContainerColorRect;
    }

    public void RemoveAvatar(Player player)
    {
        if (_playerAvatars.TryGetValue(player, out Control colorRect))
        {
            Logger.Debug($"TurnOrderDisplay: Removing avatar for player: {player.PlayerName}", _shouldLog);
            _hBox.RemoveChild(colorRect);
            colorRect.QueueFree();
            _playerAvatars.Remove(player);
        }
    }

    public void RevivePlayerAvatar(Player revivedPlayer, int index)
    {
        Control avatarContainer = CreateAvatarContainer(revivedPlayer);
        avatarContainer.Modulate = new Color(1, 1, 1, TransparentAlpha);
        _hBox.AddChild(avatarContainer);
        _playerAvatars[revivedPlayer] = avatarContainer;

        // Store original positions of containers that need to move
        List<(Control container, Vector2 startPos, Vector2 endPos)> animations = new();

        // Get containers that need to shift
        for (int i = index; i < _hBox.GetChildCount() - 1; i++)
        {
            Control container = (Control)_hBox.GetChild(i);
            animations.Add((container, container.Position, container.Position with { X = container.Position.X + 50 }));
        }

        // Move revived container to correct position
        _hBox.MoveChild(avatarContainer, index);

        // Create parallel animations
        Tween tween = GetTree().CreateTween().SetParallel();

        // Fade in revived avatar
        tween.TweenProperty(avatarContainer, "modulate:a", DefaultAlpha, FadeAnimationDuration);

        // Slide affected containers
        foreach ((Control container, Vector2 start, Vector2 end) in animations)
        {
            tween.TweenProperty(container, "position", end, FadeAnimationDuration);
        }
    }

    private Control CreateAvatarContainer(Player player)
    {
        CenterContainer container = new();

        // Create the background ColorRect
        ColorRect background = new()
        {
            Color = Colors.Gray with { A = TransparentAlpha }, CustomMinimumSize = new Vector2(50, 50)
        };

        // Create the TextureRect
        TextureRect textureRect = new()
        {
            ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(50, 50),
            Texture = GetAvatarPathFromSprite(player)
        };

        container.AddChild(background);
        container.AddChild(textureRect);

        return container;
    }

    private Texture2D GetAvatarPathFromSprite(Player player)
    {
        Logger.Debug($"TurnOrderDisplay: Loading avatar texture for: {player.PlayerName}", _shouldLog);

        Sprite2D sprite = player.GetNode<Sprite2D>("Sprite2D");
        string spritePath = sprite.Texture.ResourcePath;
        string avatarPath = ConvertSpritePathToAvatarPath(spritePath);

        return LoadAvatarTexture(avatarPath);
    }

    private string ConvertSpritePathToAvatarPath(string spritePath)
    {
        return spritePath.Replace("sprite", "avatar");
    }

    private Texture2D LoadAvatarTexture(string avatarPath)
    {
        if (_avatarCache.TryGetValue(avatarPath, out Texture2D cachedTexture))
        {
            Logger.Debug($"TurnOrderDisplay: Using cached avatar texture: {avatarPath}", _shouldLog);
            return cachedTexture;
        }

        Logger.Debug($"TurnOrderDisplay: Loading new avatar texture: {avatarPath}", _shouldLog);
        Texture2D texture = ResourceManager.Load<Texture2D>(avatarPath);
        _avatarCache[avatarPath] = texture;
        return texture;
    }

    public void CycleAvatars(Player player)
    {
        Logger.Debug($"TurnOrderDisplay: Cycling avatars for player: {player.PlayerName}", _shouldLog);

        if (_hBox.GetChildCount() > 0)
        {
            CycleExistingAvatars(player);
        }
        else
        {
            Logger.Debug("TurnOrderDisplay: No existing avatars, adding new avatar", _shouldLog);
            AddAvatar(player);
        }
    }

    private void CycleExistingAvatars(Player player)
    {
        if (_playerAvatars.Count == 0)
        {
            AddAvatar(player);
            return;
        }

        Control firstAvatar = (Control)_hBox.GetChild(0);
        AnimateAvatarTransition(firstAvatar, player);
    }

    private void AnimateAvatarTransition(Control oldAvatar, Player player)
    {
        Logger.Debug("TurnOrderDisplay: Starting avatar transition animation", _shouldLog);
        CreateFadeOutAnimation(oldAvatar, () =>
        {
            RemoveOldAvatar(oldAvatar);
            AddNewAvatarWithFadeIn(player);
        });
    }

    private void CreateFadeOutAnimation(Control avatar, Action onComplete)
    {
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(avatar, "modulate:a", TransparentAlpha, FadeAnimationDuration);
        tween.TweenCallback(Callable.From(onComplete));
    }

    // public void SetPlayerVisibility(Player player, bool isVisible)
    // {
    //     if (_playerAvatars.TryGetValue(player, out Control container))
    //     {
    //         // Animate visibility transition
    //         Tween tween = GetTree().CreateTween();
    //         float targetAlpha = isVisible ? DefaultAlpha : TransparentAlpha;
    //         tween.TweenProperty(container, "modulate:a", targetAlpha, FadeAnimationDuration);
    //     }
    // }
    //
    // public bool ShouldSkipPlayer(Player player)
    // {
    //     // Skip if player is dead and their avatar is fully transparent
    //     if (_playerAvatars.TryGetValue(player, out Control container))
    //     {
    //         return player.IsDead && container.Modulate.A <= TransparentAlpha;
    //     }
    //     return false;
    // }

    private void RemoveOldAvatar(Control avatar)
    {
        Logger.Debug("TurnOrderDisplay: Removing old avatar", _shouldLog);
        _hBox.RemoveChild(avatar);
        avatar.QueueFree();

        // Remove from dictionary if present
        foreach (KeyValuePair<Player, Control> kvp in _playerAvatars.ToList())
        {
            if (kvp.Value == avatar)
            {
                _playerAvatars.Remove(kvp.Key);
            }
        }
    }

    private void AddNewAvatarWithFadeIn(Player player)
    {
        Logger.Debug($"TurnOrderDisplay: Adding new avatar with fade in for: {player.PlayerName}", _shouldLog);
        Control newAvatar = CreateNewAvatarWithFade(player);
        _hBox.AddChild(newAvatar);
        _playerAvatars[player] = newAvatar;
        AnimateFadeIn(newAvatar);
    }

    private Control CreateNewAvatarWithFade(Player player)
    {
        CenterContainer container = new() { Modulate = new Color(1, 1, 1, TransparentAlpha) };

        // Create the background ColorRect
        ColorRect background = new()
        {
            Color = Colors.Gray with { A = TransparentAlpha }, CustomMinimumSize = new Vector2(50, 50)
        };

        // Create the TextureRect
        TextureRect textureRect = new()
        {
            ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(50, 50),
            Texture = GetAvatarPathFromSprite(player)
        };

        container.AddChild(background);
        container.AddChild(textureRect);

        return container;
    }

    // TODO: Use this
    public void SetPreviewState(Player player, bool isPreview)
    {
        if (_playerAvatars.TryGetValue(player, out Control container))
        {
            // Find the ColorRect (background) within the CenterContainer
            ColorRect background = container.GetChild<ColorRect>(0);

            // Animate the background alpha
            Tween tween = GetTree().CreateTween();
            float targetAlpha = isPreview ? 0.5f : TransparentAlpha;
            tween.TweenProperty(background, "color:a", targetAlpha, FadeAnimationDuration);
        }
    }

    private void AnimateFadeIn(Control avatar)
    {
        Tween fadeInTween = GetTree().CreateTween();
        fadeInTween.TweenProperty(avatar, "modulate:a", DefaultAlpha, FadeAnimationDuration);
    }

    public override void _ExitTree()
    {
        Logger.Debug("TurnOrderDisplay: Cleaning up turn order display", _shouldLog);
        CleanupCache();
        CleanupAvatars();
    }

    private void CleanupCache()
    {
        foreach (Texture2D texture in _avatarCache.Values)
        {
            texture?.Dispose();
        }

        _avatarCache.Clear();
    }

    private void CleanupAvatars()
    {
        foreach (Control colorRect in _playerAvatars.Values)
        {
            colorRect.QueueFree();
        }

        _playerAvatars.Clear();
    }
}
