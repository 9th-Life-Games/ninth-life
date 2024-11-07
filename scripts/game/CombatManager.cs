using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class CombatManager : Node2D
{
    private readonly CombatTurnManager _combatTurnManager = new();
    private readonly CombatUiManager _combatUiManager = new();
    private bool _isInPreviewMode;
    private Player _previewedPlayer;
    private int _previewIndex = -1;
    private Player _previouslyShownPlayer;
    private bool _shouldLog = true;

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("previous_preview"))
        {
            Logger.Debug("Previous preview", _shouldLog);
            CyclePreviewBackward();
        }
        else if (Input.IsActionJustPressed("next_preview"))
        {
            CyclePreviewForward();
        }

        if (Input.IsActionJustPressed("next_turn") && _combatTurnManager.CurrentPlayer.IsAlly &&
            _combatTurnManager.CurrentPlayer.IsHandEnabled)
        {
            NextTurn();
        }
    }

    private void CyclePreviewForward()
    {
        if (!_combatTurnManager.CurrentPlayer.IsAlly)
        {
            return;
        }

        if (_previewIndex == -1)
        {
            _previewIndex = _combatTurnManager.GetCurrentPlayerIndex();
        }

        _previewIndex = (_previewIndex + 1) % _combatTurnManager.GetTurnOrderCount();
        Player playerToPreview = _combatTurnManager.GetPlayerToPreview(_previewIndex);

        StartPreview(playerToPreview);
    }

    private void CyclePreviewBackward()
    {
        if (!_combatTurnManager.CurrentPlayer.IsAlly)
        {
            return;
        }

        if (_previewIndex == -1)
        {
            _previewIndex = _combatTurnManager.GetCurrentPlayerIndex();
        }

        _previewIndex--;
        if (_previewIndex < 0)
        {
            _previewIndex = _combatTurnManager.GetTurnOrderCount() - 1;
        }

        Player playerToPreview = _combatTurnManager.GetPlayerToPreview(_previewIndex);

        StartPreview(playerToPreview);
    }

    private void StartPreview(Player playerToPreview)
    {
        Player currentPlayer = _combatTurnManager.CurrentPlayer;
        Player lastEnemy = _combatUiManager.LastEnemy;
        Logger.Debug("Starting Preview", _shouldLog);
        // If previewing current player, end preview instead
        if (playerToPreview == currentPlayer)
        {
            EndPreview(currentPlayer, true);
            return;
        }

        EndPreview(currentPlayer, playerToPreview.IsAlly);
        Logger.Debug($"Start: Showing player to preview: {playerToPreview.PlayerName}'s board", _shouldLog);
        CombatUiManager.ShowBoard(playerToPreview, true);

        _isInPreviewMode = true;
        playerToPreview.ShowPreviewIndicator(true);

        _previouslyShownPlayer = _previewedPlayer;

        _previewedPlayer = playerToPreview;

        // Disable UI elements during preview
        _combatUiManager.SetNextButtonState(false);

        switch (_previewedPlayer.IsAlly)
        {
            case true:
                Logger.Debug($"Start: Hiding current player: {currentPlayer.PlayerName}'s board", _shouldLog);
                CombatUiManager.ShowBoard(currentPlayer, false);
                CombatUiManager.ShowHand(currentPlayer, false);
                CombatUiManager.ShowHand(playerToPreview, true, true);
                break;
            case false:
                CombatUiManager.ShowHand(currentPlayer, true, true);
                if (_previouslyShownPlayer is { IsAlly: true })
                {
                    CombatUiManager.ShowHand(_previouslyShownPlayer, false);
                }

                if (playerToPreview != lastEnemy && lastEnemy != null)
                {
                    Logger.Debug($"Start: Showing player to preview: {playerToPreview.PlayerName}'s board", _shouldLog);
                    CombatUiManager.ShowBoard(playerToPreview, true);
                    Logger.Debug($"Start: Hiding last enemy: {lastEnemy.PlayerName}'s board", _shouldLog);
                    CombatUiManager.ShowBoard(lastEnemy, false);
                }

                break;
        }
    }

    private void EndPreview(Player currentPlayer, bool isAllyNext = false)
    {
        if (!_isInPreviewMode)
        {
            return;
        }

        Logger.Debug("Ending Preview", _shouldLog);

        Player lastEnemy = _combatUiManager.LastEnemy;
        Player firstEnemy = _combatTurnManager.FirstEnemy;

        _previewedPlayer.ShowPreviewIndicator(false);

        CombatUiManager.ShowBoard(currentPlayer, true);

        if (lastEnemy != null)
        {
            Logger.Debug($"End: Showing last enemy: {lastEnemy.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(lastEnemy, true);
        }

        if (_previewedPlayer.IsAlly && _previewedPlayer != currentPlayer)
        {
            CombatUiManager.ShowHand(_previewedPlayer, false);
        }

        // Re-enable UI elements
        _combatUiManager.SetNextButtonState(true);
        if (currentPlayer.IsAlly)
        {
            CombatUiManager.ShowHand(currentPlayer, true);
        }

        if (_previewedPlayer != null)
        {
            Logger.Debug($"End: Hiding previewed player: {_previewedPlayer.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(_previewedPlayer, false);
        }

        if (!_previewedPlayer.IsAlly && isAllyNext)
        {
            Logger.Debug(
                $"End: Showing {(lastEnemy != null ? "last enemy" : "first enemy")}: {(lastEnemy != null ? lastEnemy.PlayerName : firstEnemy.PlayerName)}'s board",
                _shouldLog
            );
            CombatUiManager.ShowBoard(lastEnemy ?? firstEnemy, true);
        }
        else if (_previewedPlayer == lastEnemy && isAllyNext)
        {
            Logger.Debug("Not getting triggered", _shouldLog);
        }
        else if (_previewedPlayer.IsAlly && !isAllyNext)
        {
            Logger.Debug("This is the condition that's not handling stuff right", _shouldLog);
            Logger.Debug($"End: Hiding first enemy: {firstEnemy.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(firstEnemy, false);
        }

        _isInPreviewMode = false;
        _previewedPlayer = null;
        _previouslyShownPlayer = null;
    }

    public override void _Ready()
    {
        _combatUiManager.SetNextButton(GetNode<Button>("../Button"));
        _combatTurnManager.InitializePlayers(this);
        _combatTurnManager.SetInitialAlliesAndEnemies(GetChildren().OfType<Player>());
        _combatUiManager.InitPlayerUi(
            _combatTurnManager.CurrentPlayer,
            _combatTurnManager.FirstAlly,
            _combatTurnManager.FirstEnemy
        );
        if (!_combatTurnManager.CurrentPlayer.IsAlly)
        {
            InitEnemyAi();
        }
    }

    public void NextTurn()
    {
        Player currentPlayer = _combatTurnManager.CurrentPlayer;
        // Disconnect the event handler from current player if it's an enemy
        if (!currentPlayer.IsAlly)
        {
            currentPlayer.EnemyFinishedTurn -= OnEnemyFinishedTurn;
        }

        // Get the next player
        Player nextPlayer = _combatTurnManager.GetNextPlayer();

        // Handle current player's exit
        _combatUiManager.HandleCurrentAllyHandExit(currentPlayer, nextPlayer);


        // Hide lastAlly hand to swap with nextAlly hand as it gets shown
        if (!currentPlayer.IsAlly && nextPlayer.IsAlly)
        {
            Player lastAlly = _combatTurnManager.GetLastAlly();
            CombatUiManager.HideLastAllyHand(lastAlly);
        }

        // Update and handle enemy board transitions
        _combatUiManager.HideLastPlayerBoard(nextPlayer.IsAlly);

        // End current players turn and start next players turn
        currentPlayer = _combatTurnManager.SwapTurnToNextPlayer(nextPlayer);

        // Handle new player's entrance
        if (!currentPlayer.IsAlly)
        {
            currentPlayer.EnemyFinishedTurn += OnEnemyFinishedTurn;
        }

        // Handle UI turn change
        _combatUiManager.ShowPlayerUi(currentPlayer);

        currentPlayer.StartTurn();
    }

    private void InitEnemyAi()
    {
        GetTree().CreateTimer(1).Timeout += _combatTurnManager.CurrentPlayer.EnemyPlayHand;
        _combatTurnManager.CurrentPlayer.EnemyFinishedTurn += OnEnemyFinishedTurn;
    }

    private void OnEnemyFinishedTurn()
    {
        GetTree().CreateTimer(1.5).Timeout += NextTurn;
    }
}
