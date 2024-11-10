using System;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class CombatManager : Node2D
{
    private readonly CombatTurnManager _combatTurnManager = new();
    private readonly CombatUiManager _combatUiManager = new();
    private Button _attackButton;
    private Node2D _defenseDraw;
    private Button _endTurnButton;
    private bool _hasAttacked;
    private bool _isInAttackMode;
    private bool _isInPreviewMode;
    private Player _playerToAttack;
    private int _playerToAttackIndex;
    private Player _previewedPlayer;
    private int _previewIndex = -1;
    private Player _previouslyShownPlayer;
    private Node2D _rollIndicator;
    private int _rollValue;
    private bool _shouldLog;
    private TurnOrderDisplay _turnOrderDisplay;

    public override void _Input(InputEvent @event)
    {
        if (_isInAttackMode)
        {
            if (Input.IsActionPressed("accept"))
            {
                _isInAttackMode = false;
                _attackButton.Disabled = true;
                _hasAttacked = true;
                RollForAttack();
                _playerToAttack.DrawCardForDefense(() =>
                {
                    _endTurnButton.Disabled = false;
                    _combatTurnManager.EnemyTurnOrder.ForEach(player =>
                    {
                        player.SetSelectedEnemy(false);
                        player.SetAttackModeIndicator(false);
                        if (_combatTurnManager.CurrentPlayer.CurrentCardPlays <
                            _combatTurnManager.CurrentPlayer.TotalCardPlays)
                        {
                            CombatUiManager.ShowHand(_combatTurnManager.CurrentPlayer, true);
                        }
                    });
                });
            }

            if (Input.IsActionJustPressed("previous_preview"))
            {
                CycleAttackModeBackward();
            }
            else if (Input.IsActionJustPressed("next_preview"))
            {
                CycleAttackModeForward();
            }
        }
        else
        {
            if (Input.IsActionJustPressed("previous_preview"))
            {
                CyclePreviewBackward();
            }
            else if (Input.IsActionJustPressed("next_preview"))
            {
                CyclePreviewForward();
            }
        }


        if (Input.IsActionJustPressed("next_turn") && _combatTurnManager.CurrentPlayer.IsAlly &&
            _combatTurnManager.CurrentPlayer.IsHandEnabled)
        {
            NextTurn();
        }
    }

    public override void _Ready()
    {
        _turnOrderDisplay = GetNode<TurnOrderDisplay>("../TurnOrderDisplay");
        _endTurnButton = GetNode<Button>("../EndTurn");
        _rollIndicator = GetNode<Node2D>("../RollIndicator");
        _attackButton = GetNode<Button>("../Attack");
        _defenseDraw = GetNode<Node2D>("../DefenseDraw");
        _defenseDraw.ChildEnteredTree += OnChildEnteredTree;
        _attackButton.Toggled += AttackButtonOnPressed;
        _combatUiManager.SetButtons(_endTurnButton, _attackButton);
        _combatTurnManager.InitializePlayers(this, _turnOrderDisplay);
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

    private void OnChildEnteredTree(Node node)
    {
        ((Card)node).CardLeftTree += DiscardDefenseCard;
    }

    private void DiscardDefenseCard(Card card)
    {
        _playerToAttack.PlayerBoard.AddCard(card, true);
    }

    private void AttackButtonOnPressed(bool pressed)
    {
        _endTurnButton.Disabled = pressed;
        _isInAttackMode = pressed;
        _combatTurnManager.EnemyTurnOrder.ForEach(player => player.SetAttackModeIndicator(pressed));
        if (!pressed)
        {
            if (_combatTurnManager.CurrentPlayer.CurrentCardPlays < _combatTurnManager.CurrentPlayer.TotalCardPlays)
            {
                CombatUiManager.ShowHand(_combatTurnManager.CurrentPlayer, true);
            }

            _playerToAttack?.SetSelectedEnemy(false);
        }
        else
        {
            Logger.Debug("Toggled false");
            _combatTurnManager.FirstEnemy.SetSelectedEnemy(true);
            _playerToAttack = _combatTurnManager.FirstEnemy;
            _playerToAttackIndex = 0;
            _combatTurnManager.CurrentPlayer.SlideHandDisabled();
        }
    }

    private void RollForAttack()
    {
        Random random = new();
        int randomNumber = random.Next(1, 11);
        _rollValue = randomNumber + _combatTurnManager.CurrentPlayer.MasteryBonus;
        _rollIndicator.GetNode<Label>("Label").Text = $"{_rollValue}";
        _rollIndicator.GetNode<AnimationPlayer>("AnimationPlayer").Play("show_result");
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


    private void CycleAttackModeBackward()
    {
        if (_playerToAttackIndex <= 0)
        {
            _playerToAttack.SetSelectedEnemy(false);
            _playerToAttackIndex = _combatTurnManager.EnemyTurnOrder.Count - 1;
        }
        else
        {
            _playerToAttack.SetSelectedEnemy(false);
            _playerToAttackIndex--;
        }

        _playerToAttack = _combatTurnManager.EnemyTurnOrder[_playerToAttackIndex];
        _playerToAttack.SetSelectedEnemy(true);
    }

    private void CycleAttackModeForward()
    {
        if (_playerToAttackIndex >= _combatTurnManager.EnemyTurnOrder.Count - 1)
        {
            _playerToAttack.SetSelectedEnemy(false);
            _playerToAttackIndex = 0;
        }
        else
        {
            _playerToAttack.SetSelectedEnemy(false);
            _playerToAttackIndex++;
        }

        _playerToAttack = _combatTurnManager.EnemyTurnOrder[_playerToAttackIndex];
        _playerToAttack.SetSelectedEnemy(true);
    }

    private void StartPreview(Player playerToPreview)
    {
        Player currentPlayer = _combatTurnManager.CurrentPlayer;
        Player firstEnemy = _combatTurnManager.FirstEnemy;
        Player lastEnemy = _combatUiManager.LastEnemy;
        Logger.Debug("Starting Preview", _shouldLog);
        // If previewing current player, end preview instead
        if (playerToPreview == currentPlayer)
        {
            EndPreview(currentPlayer, true);
            return;
        }

        EndPreview(currentPlayer, playerToPreview.IsAlly);
        Logger.Debug($"Start: [1] Showing player to preview: {playerToPreview.PlayerName}'s board", _shouldLog);
        CombatUiManager.ShowBoard(playerToPreview, true);
        if (_previouslyShownPlayer == null && lastEnemy == null && !playerToPreview.IsAlly &&
            playerToPreview != firstEnemy)
        {
            CombatUiManager.ShowBoard(firstEnemy, false);
        }

        _isInPreviewMode = true;
        currentPlayer.TurnIndicatorPreviewColor(true);
        playerToPreview.ShowPreviewIndicator(true);

        _previouslyShownPlayer = _previewedPlayer;

        _previewedPlayer = playerToPreview;

        // Disable UI elements during preview
        _combatUiManager.SetNextButtonState(false);
        _combatUiManager.SetAttackButtonState(false);

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
                    Logger.Debug($"Start: [2] Showing player to preview: {playerToPreview.PlayerName}'s board",
                        _shouldLog);
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
            Logger.Debug($"End: [1] Showing last enemy: {lastEnemy.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(lastEnemy, true);
        }

        if (_previewedPlayer.IsAlly && _previewedPlayer != currentPlayer)
        {
            CombatUiManager.ShowHand(_previewedPlayer, false);
        }

        // Re-enable UI elements
        _combatUiManager.SetNextButtonState(true);
        if (!_hasAttacked)
        {
            _combatUiManager.SetAttackButtonState(true);
        }

        if (currentPlayer.IsAlly)
        {
            if (currentPlayer.CurrentCardPlays >= currentPlayer.TotalCardPlays)
            {
                CombatUiManager.ShowHand(currentPlayer, true, true);
            }
            else
            {
                CombatUiManager.ShowHand(currentPlayer, true);
            }
        }

        if (_previewedPlayer != null)
        {
            Logger.Debug("This is the condition that's not handling stuff right", _shouldLog);
            Logger.Debug($"End: Hiding previewed player: {_previewedPlayer.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(_previewedPlayer, false);
        }

        if (!_previewedPlayer.IsAlly && isAllyNext)
        {
            Logger.Debug(
                $"End: [2] Showing {(lastEnemy != null ? "last enemy" : "first enemy")}: {(lastEnemy != null ? lastEnemy.PlayerName : firstEnemy.PlayerName)}'s board",
                _shouldLog
            );
            CombatUiManager.ShowBoard(lastEnemy ?? firstEnemy, true);
        }
        else if (_previewedPlayer.IsAlly && !isAllyNext)
        {
            Logger.Debug($"End: Hiding first enemy: {firstEnemy.PlayerName}'s board", _shouldLog);
            CombatUiManager.ShowBoard(firstEnemy, false);
        }

        _isInPreviewMode = false;
        currentPlayer.TurnIndicatorPreviewColor(false);
        _previewedPlayer = null;
        _previouslyShownPlayer = null;
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

        _hasAttacked = false;
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
