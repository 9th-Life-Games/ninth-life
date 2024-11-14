using System;
using System.Collections.Generic;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class TurnOrderDisplay : Control
{
    private const float FadeAnimationDuration = 0.2f;
    private const float DefaultAlpha = 1.0f;
    private const float TransparentAlpha = 0.0f;

    private readonly Dictionary<string, Texture2D> _avatarCache = new();
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
        TextureRect avatarContainer = CreateAvatarContainer(player);
        _hBox.AddChild(avatarContainer);
    }

    private TextureRect CreateAvatarContainer(Player player)
    {
        return new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = GetAvatarPathFromSprite(player)
        };
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

    private void RemoveOldAvatar(Control avatar)
    {
        Logger.Debug("TurnOrderDisplay: Removing old avatar", _shouldLog);
        _hBox.RemoveChild(avatar);
        avatar.QueueFree();
    }

    private void AddNewAvatarWithFadeIn(Player player)
    {
        Logger.Debug($"TurnOrderDisplay: Adding new avatar with fade in for: {player.PlayerName}", _shouldLog);
        TextureRect newAvatar = CreateNewAvatarWithFade(player);
        _hBox.AddChild(newAvatar);
        AnimateFadeIn(newAvatar);
    }

    private TextureRect CreateNewAvatarWithFade(Player player)
    {
        return new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = GetAvatarPathFromSprite(player),
            Modulate = new Color(1, 1, 1, TransparentAlpha)
        };
    }

    private void AnimateFadeIn(TextureRect avatar)
    {
        Tween fadeInTween = GetTree().CreateTween();
        fadeInTween.TweenProperty(avatar, "modulate:a", DefaultAlpha, FadeAnimationDuration);
    }

    public override void _ExitTree()
    {
        Logger.Debug("TurnOrderDisplay: Cleaning up turn order display", _shouldLog);
        CleanupCache();
    }

    private void CleanupCache()
    {
        foreach (Texture2D texture in _avatarCache.Values)
        {
            texture?.Dispose();
        }

        _avatarCache.Clear();
    }
}
