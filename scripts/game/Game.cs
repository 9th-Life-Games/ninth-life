using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class Game : Node2D
{
    private readonly bool _shouldLog = true;

    private CombatManager _combatManager;
    private Button _endTurnButton;
    private AudioStreamPlayer2D _musicPlayer;

    public override void _Ready()
    {
        Logger.Debug("Game: Initializing game", _shouldLog);
        InitializeComponents();
        SetupEventHandlers();
        StartBackgroundMusic();
    }

    private void InitializeComponents()
    {
        Logger.Debug("Game: Setting up game components", _shouldLog);
        _endTurnButton = GetNode<Button>("EndTurn");
        _combatManager = GetNode<CombatManager>("CombatManager");
        _musicPlayer = GetNode<AudioStreamPlayer2D>("MusicPlayer");
    }

    private void SetupEventHandlers()
    {
        Logger.Debug("Game: Setting up event handlers", _shouldLog);
        _endTurnButton.Pressed += EndTurnButtonOnPressed;
    }

    private void StartBackgroundMusic()
    {
        Logger.Debug("Game: Starting background music", _shouldLog);
        _musicPlayer?.Play();
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("quit"))
        {
            Logger.Debug("Game: Quit action detected", _shouldLog);
            CleanupAndQuit();
        }
    }

    private void EndTurnButtonOnPressed()
    {
        Logger.Debug("Game: End turn button pressed", _shouldLog);
        _combatManager.NextTurn();
    }

    private void CleanupAndQuit()
    {
        Logger.Debug("Game: Starting game cleanup process", _shouldLog);
        CleanupCombatEntities();
        StopBackgroundMusic();
        CleanupResources();
        ExitGame();
    }

    private void CleanupCombatEntities()
    {
        Logger.Debug($"Game: Cleaning up {GameUtils.CombatEntities.Count} combat entities", _shouldLog);
        foreach (Player player in GameUtils.CombatEntities)
        {
            Logger.Debug($"Game: Freeing player: {player.PlayerName}", _shouldLog);
            player.QueueFree();
        }

        GameUtils.CombatEntities.Clear();
    }

    private void StopBackgroundMusic()
    {
        if (_musicPlayer != null)
        {
            Logger.Debug("Game: Stopping background music", _shouldLog);
            _musicPlayer.Stop();
        }
    }

    private void CleanupResources()
    {
        Logger.Debug("Game: Cleaning up game resources", _shouldLog);
        ResourceManager.Cleanup();
    }

    private void ExitGame()
    {
        Logger.Debug("Game: Exiting game", _shouldLog);
        GetTree().Quit();
    }
}
