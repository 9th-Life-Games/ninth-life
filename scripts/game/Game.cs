using Godot;

namespace NinthLife.scripts.game;

public partial class Game : Node2D
{
    private Button _button;

    private CombatManager _combatManager;
    // private Button _fanButton;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape }) GetTree().Quit();
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _button = GetNode<Button>("Button");
        // _fanButton = GetNode<Button>("FanButton");
        _combatManager = GetNode<CombatManager>("CombatManager");
        _button.Pressed += ButtonOnPressed;
        // _fanButton.Pressed += FanButtonOnPressed;
    }

    private void FanButtonOnPressed()
    {
        _combatManager.CurrentPlayer.SlideHandIn();
    }

    private void ButtonOnPressed()
    {
        _combatManager.NextTurn();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}