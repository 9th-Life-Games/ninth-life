using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class Game : Node2D
{
    private Button _button;

    private CombatManager _combatManager;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            CleanupAndQuit();
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _button = GetNode<Button>("Button");
        _combatManager = GetNode<CombatManager>("CombatManager");
        _button.Pressed += ButtonOnPressed;
    }

    private void ButtonOnPressed()
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

        // Clean up resources
        ResourceManager.Cleanup();

        GetTree().Quit();
    }
}
