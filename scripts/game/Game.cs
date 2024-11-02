using Godot;

namespace NinthLife.scripts.game
{
    public partial class Game : Node2D
    {
        private Button _button;

        private CombatManager _combatManager;

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
            {
                GetTree().Quit();
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

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta) { }
    }
}
