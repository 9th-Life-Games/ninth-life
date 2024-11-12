using System.Collections.Generic;
using Godot;

namespace NinthLife.scripts.game.combat;

public class CombatModeManager
{
    public enum CombatModeType
    {
        None,
        Preview,
        Attack
    }

    private readonly Dictionary<CombatModeType, ICombatMode> _modes;

    public CombatModeManager(CombatManager combatManager, CombatUiManager uiManager, CombatTurnManager turnManager)
    {
        _modes = new Dictionary<CombatModeType, ICombatMode>
        {
            { CombatModeType.Preview, new PreviewMode(combatManager, uiManager, turnManager) },
            { CombatModeType.Attack, new AttackMode(combatManager, uiManager, turnManager) }
        };
    }

    public ICombatMode CurrentMode { get; private set; }

    public void EnterMode(CombatModeType modeType, bool isForward = true)
    {
        ExitCurrentMode();

        if (modeType == CombatModeType.None)
        {
            return;
        }

        CurrentMode = _modes[modeType];
        if (CurrentMode is PreviewMode previewMode)
        {
            previewMode.Enter(isForward);
        }
        else
        {
            CurrentMode.Enter();
        }
    }

    public void ExitCurrentMode()
    {
        CurrentMode?.Exit();
        CurrentMode = null;
    }

    public void Update(InputEvent inputEvent)
    {
        CurrentMode?.Update(inputEvent);
    }
}
