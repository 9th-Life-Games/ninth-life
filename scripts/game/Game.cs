using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class Game : Node2D
{
    private CombatManager _combatManager;
    private Button _endTurnButton;
    private AudioStreamPlayer2D _musicPlayer;

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("quit"))
        {
            CleanupAndQuit();
        }
    }

    public override void _Ready()
    {
        _endTurnButton = GetNode<Button>("EndTurn");
        _combatManager = GetNode<CombatManager>("CombatManager");
        _endTurnButton.Pressed += EndTurnButtonOnPressed;
        _musicPlayer = GetNode<AudioStreamPlayer2D>("MusicPlayer");
        _musicPlayer.Play();
    }

    private void EndTurnButtonOnPressed()
    {
        _combatManager.NextTurn();
    }

    private void CleanupAndQuit()
    {
        Logger.Debug("Game cleanup");
        // Clean up all game entities
        foreach (Player player in GameUtils.CombatEntities)
        {
            player.QueueFree();
        }

        GameUtils.CombatEntities.Clear();

        if (_musicPlayer != null)
        {
            _musicPlayer.Stop();
        }

        // Clean up resources
        ResourceManager.Cleanup();

        GetTree().Quit();
    }
}
