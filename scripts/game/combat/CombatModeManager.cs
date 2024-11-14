using System.Collections.Generic;
using Godot;
using NinthLife.scripts.utils;

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
    private readonly bool _shouldLog = true;

    public CombatModeManager(CombatManager combatManager, CombatUiManager uiManager, CombatTurnManager turnManager)
    {
        Logger.Debug("CombatModeManager: Initializing combat modes", _shouldLog);
        _modes = new Dictionary<CombatModeType, ICombatMode>
        {
            { CombatModeType.Preview, new PreviewMode(combatManager, uiManager, turnManager) },
            { CombatModeType.Attack, new AttackMode(combatManager, uiManager, turnManager) }
        };
        SetupPreviewModeHandler();
    }

    public ICombatMode CurrentMode { get; private set; }

    private void SetupPreviewModeHandler()
    {
        if (_modes[CombatModeType.Preview] is PreviewMode previewMode)
        {
            Logger.Debug("CombatModeManager: Setting up preview mode handler", _shouldLog);
            previewMode.PreviewEnded += OnPreviewEnded;
        }
    }

    private void OnPreviewEnded()
    {
        Logger.Debug("CombatModeManager: Preview ended, switching to None mode", _shouldLog);
        EnterMode(CombatModeType.None);
    }

    public void EnterMode(CombatModeType modeType, bool isForward = true, Player playerToPreview = null)
    {
        Logger.Debug($"CombatModeManager: Entering mode: {modeType}", _shouldLog);

        if (ShouldExitCurrentMode(modeType, playerToPreview))
        {
            ExitCurrentMode();
        }

        if (modeType == CombatModeType.None)
        {
            Logger.Debug("CombatModeManager: Entering None mode, exiting current mode", _shouldLog);
            ExitCurrentMode();
            return;
        }

        InitializeNewMode(modeType, isForward, playerToPreview);
    }

    private bool ShouldExitCurrentMode(CombatModeType newMode, Player playerToPreview)
    {
        return playerToPreview != null || (CurrentMode != null && newMode == CombatModeType.None);
    }

    private void InitializeNewMode(CombatModeType modeType, bool isForward, Player playerToPreview)
    {
        Logger.Debug($"CombatModeManager: Initializing new mode: {modeType}", _shouldLog);
        CurrentMode = _modes[modeType];

        if (CurrentMode is PreviewMode previewMode)
        {
            Logger.Debug(
                $"CombatModeManager: Entering preview mode, forward: {isForward}, player: {playerToPreview?.PlayerName ?? "none"}",
                _shouldLog);
            previewMode.Enter(isForward, playerToPreview);
        }
        else
        {
            Logger.Debug($"CombatModeManager: Entering non-preview mode: {modeType}, forward: {isForward}", _shouldLog);
            CurrentMode.Enter(isForward);
        }
    }

    public void ExitCurrentMode()
    {
        if (CurrentMode == null)
        {
            Logger.Debug("CombatModeManager: No current mode to exit", _shouldLog);
            return;
        }

        Logger.Debug($"CombatModeManager: Exiting current mode: {CurrentMode.GetType().Name}", _shouldLog);
        CurrentMode.Exit();
        CurrentMode = null;
    }

    public void Update(InputEvent inputEvent)
    {
        if (CurrentMode != null)
        {
            CurrentMode.Update(inputEvent);
        }
    }
}
