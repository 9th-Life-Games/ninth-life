using Godot;

namespace NinthLife.scripts.game.combat;

public interface ICombatMode
{
    void Enter(bool isForward = true);
    void Exit();
    void Update(InputEvent inputEvent);
}
