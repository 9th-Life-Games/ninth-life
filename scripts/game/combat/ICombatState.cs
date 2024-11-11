namespace NinthLife.scripts.game.combat;

public interface ICombatState
{
    void Enter();
    void Exit();
    void Update();
    bool CanTransitionTo(ICombatState nextState);
}
