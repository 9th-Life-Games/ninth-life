using Godot;
using NinthLife.scripts.game.combat;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class Game : Node2D
{
    private CombatManager _combatManager;
    private Button _endTurnButton;

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
    }

    private void EndTurnButtonOnPressed()
    {
        // Only allow ending turn if it's an ally's turn and their hand is enabled
        TurnManager turnManager = _combatManager.TurnManager;
        Player currentPlayer = turnManager.CurrentPlayer;

        if (currentPlayer.IsAlly && currentPlayer.IsHandEnabled && !turnManager.IsInAttackMode)
        {
            turnManager.TransitionTo<EndTurnState>();
        }
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
