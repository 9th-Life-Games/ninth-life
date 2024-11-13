using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.game.combat;

namespace NinthLife.scripts.game;

public partial class CombatManager : Node2D
{
    private readonly CombatTurnManager _combatTurnManager = new();
    private readonly CombatUiManager _combatUiManager = new();
    private Button _attackButton;
    private Node2D _defenseDraw;
    private Button _endTurnButton;
    private bool _isInAttackMode;
    private bool _isInPreviewMode;
    private CombatModeManager _modeManager;
    private Player _playerToAttack;
    private int _playerToAttackIndex;
    private Player _previewedPlayer;
    private int _previewIndex = -1;
    private Player _previouslyShownPlayer;
    private Node2D _rollIndicator;
    private int _rollValue;
    private bool _shouldLog;
    private TurnOrderDisplay _turnOrderDisplay;
    public AudioStreamPlayer2D AttackSound { get; private set; }
    private Player FirstAlly { get; set; }
    public Player FirstEnemy { get; private set; }
    public bool HasAttacked { get; private set; }

    public override void _Input(InputEvent @event)
    {
        _modeManager.Update(@event);
        if (_modeManager.CurrentMode is not AttackMode && _modeManager.CurrentMode is not PreviewMode)
        {
            if (Input.IsActionPressed("previous_preview"))
            {
                _modeManager.EnterMode(CombatModeManager.CombatModeType.Preview, false);
            }
            else if (Input.IsActionPressed("next_preview"))
            {
                _modeManager.EnterMode(CombatModeManager.CombatModeType.Preview);
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
        InitializeNodes();
        InitializeComponents();
        SetupEventHandlers();
    }

    private void InitializeNodes()
    {
        _turnOrderDisplay = GetNode<TurnOrderDisplay>("../TurnOrderDisplay");
        _endTurnButton = GetNode<Button>("../EndTurn");
        _rollIndicator = GetNode<Node2D>("../RollIndicator");
        _attackButton = GetNode<Button>("../Attack");
        _defenseDraw = GetNode<Node2D>("../DefenseDraw");
        AttackSound = GetNode<AudioStreamPlayer2D>("../AttackSound");

        // Set up UI manager with required references
        _combatUiManager.SetButtons(_endTurnButton, _attackButton);
    }

    private void InitializeComponents()
    {
        // Initialize combat entities
        _combatTurnManager.InitializePlayers(this, _turnOrderDisplay);
        SetInitialAlliesAndEnemies(GetChildren().OfType<Player>());

        // Initialize UI for first turn
        _combatUiManager.InitPlayerUi(
            _combatTurnManager.CurrentPlayer,
            FirstAlly,
            FirstEnemy
        );

        _modeManager = new CombatModeManager(this, _combatUiManager, _combatTurnManager);

        if (!_combatTurnManager.CurrentPlayer.IsAlly)
        {
            InitEnemyAi();
        }
    }

    private void SetupEventHandlers()
    {
        _defenseDraw.ChildEnteredTree += OnDefenseDrawChildEnteredTree;
        _attackButton.Toggled += AttackButtonOnPressed;
    }

    private void SetInitialAlliesAndEnemies(IEnumerable<Player> players)
    {
        foreach (Player player in players)
        {
            if (player.IsAlly)
            {
                FirstAlly ??= player;
            }
            else
            {
                FirstEnemy ??= player;
            }
        }
    }

    private void OnDefenseDrawChildEnteredTree(Node node)
    {
        ((Card)node).CardLeftTree += DiscardDefenseCard;
    }

    private void DiscardDefenseCard(Card card)
    {
        AttackMode currentMode = _modeManager.CurrentMode as AttackMode;
        currentMode?.TargetPlayer.PlayerBoard.AddCard(card, true);
        ExitCurrentMode();
    }

    private void AttackButtonOnPressed(bool pressed)
    {
        _modeManager.EnterMode(
            pressed ? CombatModeManager.CombatModeType.Attack : CombatModeManager.CombatModeType.None);
    }

    public void ExecuteAttack(Player target)
    {
        _attackButton.Disabled = true;
        HasAttacked = true;
        RollForAttack();

        Card drawnCard = target.DrawCardForDefense(() =>
        {
            _endTurnButton.Disabled = false;
            if (_combatTurnManager.CurrentPlayer.CurrentCardPlays < _combatTurnManager.CurrentPlayer.TotalCardPlays)
            {
                CombatUiManager.ShowHand(_combatTurnManager.CurrentPlayer, true);
            }
        });

        int drawnValue = 0;
        if (_combatTurnManager.CurrentPlayer.IsAlly)
        {
            drawnValue = drawnCard.NumericValue;
        }
        else
        {
            drawnValue = drawnCard.NumericValue > 11 ? 10 : drawnCard.NumericValue;
        }

        if (drawnValue > _rollValue)
        {
            target.PlayBlockAnimation();
        }
        else
        {
            target.PlayHitAnimation();
        }
    }

    private void ExitCurrentMode()
    {
        _modeManager.ExitCurrentMode();
    }

    private void RollForAttack()
    {
        Random random = new();
        int randomNumber = random.Next(1, 11);
        _rollValue = randomNumber + _combatTurnManager.CurrentPlayer.MasteryBonus;
        _rollIndicator.GetNode<Label>("Label").Text = $"{_rollValue}";
        _rollIndicator.GetNode<AnimationPlayer>("AnimationPlayer").Play("show_result");
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

        HasAttacked = false;
        currentPlayer.StartTurn();
    }

    private void InitEnemyAi()
    {
        GetTree().CreateTimer(1).Timeout += _combatTurnManager.CurrentPlayer.EnemyPlayHand;
        _combatTurnManager.CurrentPlayer.EnemyFinishedTurn += OnEnemyFinishedTurn;
    }

    private void OnEnemyFinishedTurn()
    {
        // Create a Random instance
        Random random = new();

        // Generate a random index between 0 and the number of allies minus 1
        int randomAllyIndex = random.Next(0, _combatTurnManager.AllyTurnOrder.Count);

        // Execute attack on the randomly selected ally
        GetTree().CreateTimer(1.75).Timeout += () => ExecuteAttack(_combatTurnManager.AllyTurnOrder[randomAllyIndex]);

        // Continue with turn transition after delay
        GetTree().CreateTimer(4).Timeout += NextTurn;
    }
}
