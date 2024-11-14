using System;
using Godot;
using NinthLife.scripts.game.combat;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class CombatManager : Node2D
{
    private readonly CombatTurnManager _combatTurnManager = new();
    private readonly CombatUiManager _combatUiManager = new();
    private readonly bool _shouldLog = true;

    private Button _attackButton;
    private Node2D _defenseDraw;
    private Button _endTurnButton;
    private CombatModeManager _modeManager;
    private Node2D _rollIndicator;
    private int _rollValue;
    private TurnOrderDisplay _turnOrderDisplay;

    public AudioStreamPlayer2D AttackSound { get; private set; }
    private Player FirstAlly { get; set; }
    public Player FirstEnemy { get; private set; }
    public bool HasAttacked { get; private set; }

    public override void _Ready()
    {
        InitializeNodes();
        InitializeComponents();
        SetupEventHandlers();
    }

    public override void _Input(InputEvent @event)
    {
        _modeManager.Update(@event);
        HandleTurnInput();
        HandlePreviewInput();
    }

    private void HandleTurnInput()
    {
        if (Input.IsActionJustPressed("next_turn") &&
            _combatTurnManager.CurrentPlayer.IsAlly &&
            _combatTurnManager.CurrentPlayer.IsHandEnabled)
        {
            Logger.Debug("Next turn input received from enabled ally", _shouldLog);
            NextTurn();
        }
    }

    private void HandlePreviewInput()
    {
        bool canEnterPreview = _modeManager.CurrentMode is not AttackMode &&
                               _modeManager.CurrentMode is not PreviewMode &&
                               _combatTurnManager.CurrentPlayer.IsAlly &&
                               _combatTurnManager.CurrentPlayer.IsHandEnabled;

        if (!canEnterPreview)
        {
            return;
        }

        if (Input.IsActionPressed("previous_preview"))
        {
            Logger.Debug("Entering preview mode (backward)", _shouldLog);
            _modeManager.EnterMode(CombatModeManager.CombatModeType.Preview, false);
        }
        else if (Input.IsActionPressed("next_preview"))
        {
            Logger.Debug("Entering preview mode (forward)", _shouldLog);
            _modeManager.EnterMode(CombatModeManager.CombatModeType.Preview);
        }
    }

    private void InitializeNodes()
    {
        Logger.Debug("Initializing combat manager nodes", _shouldLog);
        _turnOrderDisplay = GetNode<TurnOrderDisplay>("../TurnOrderDisplay");
        _endTurnButton = GetNode<Button>("../EndTurn");
        _rollIndicator = GetNode<Node2D>("../RollIndicator");
        _attackButton = GetNode<Button>("../Attack");
        _defenseDraw = GetNode<Node2D>("../DefenseDraw");
        AttackSound = GetNode<AudioStreamPlayer2D>("../AttackSound");

        _combatUiManager.SetButtons(_endTurnButton, _attackButton);
    }

    private void InitializeComponents()
    {
        Logger.Debug("Initializing combat components", _shouldLog);
        _combatTurnManager.InitializePlayers(this, _turnOrderDisplay);
        SetInitialAlliesAndEnemies();

        Logger.Debug("Initializing UI for first turn", _shouldLog);
        _combatUiManager.InitPlayerUi(
            _combatTurnManager.CurrentPlayer,
            FirstAlly,
            FirstEnemy
        );

        _modeManager = new CombatModeManager(this, _combatUiManager, _combatTurnManager);

        if (!_combatTurnManager.CurrentPlayer.IsAlly)
        {
            Logger.Debug("Initializing enemy AI", _shouldLog);
            InitEnemyAi();
        }
        else
        {
            Logger.Debug("Enabling player buttons for ally turn", _shouldLog);
            SetAllPlayerButtonsEnabled(true);
        }
    }

    private void SetupEventHandlers()
    {
        Logger.Debug("Setting up event handlers", _shouldLog);
        _defenseDraw.ChildEnteredTree += OnDefenseDrawChildEnteredTree;
        _attackButton.Toggled += AttackButtonOnPressed;
    }

    private void SetInitialAlliesAndEnemies()
    {
        Logger.Debug("Setting up initial allies and enemies", _shouldLog);
        _combatTurnManager.TurnOrder.ForEach(player =>
        {
            Logger.Debug($"Setting up player: {player.PlayerName}", _shouldLog);
            player.PlayerClicked += OnPlayerClicked;
            player.AllyTurnReady += OnAllyHandEnabled;

            if (player.IsAlly)
            {
                if (FirstAlly == null)
                {
                    Logger.Debug($"Set first ally: {player.PlayerName}", _shouldLog);
                    FirstAlly = player;
                }
            }
            else
            {
                if (FirstEnemy == null)
                {
                    Logger.Debug($"Set first enemy: {player.PlayerName}", _shouldLog);
                    FirstEnemy = player;
                }
            }
        });

        SetAllPlayerButtonsEnabled(false);
    }

    private void OnAllyHandEnabled()
    {
        Logger.Debug("Ally hand enabled, enabling player buttons", _shouldLog);
        SetAllPlayerButtonsEnabled(true);
    }

    private void SetAllPlayerButtonsEnabled(bool enabled)
    {
        _combatTurnManager.TurnOrder.ForEach(player =>
        {
            Logger.Debug($"Setting {player.PlayerName}'s button enabled: {enabled}", _shouldLog);
            player.SetButtonEnabled(enabled);
        });
    }

    private void OnPlayerClicked(Player player)
    {
        if (_modeManager.CurrentMode is not AttackMode && _modeManager.CurrentMode is not PreviewMode)
        {
            Logger.Debug($"Player clicked: {player.PlayerName}, entering preview mode", _shouldLog);
            _modeManager.EnterMode(CombatModeManager.CombatModeType.Preview, false, player);
        }
    }

    private void OnDefenseDrawChildEnteredTree(Node node)
    {
        Logger.Debug("Defense draw card entered tree, setting up discard handler", _shouldLog);
        ((Card)node).CardLeftTree += DiscardDefenseCard;
    }

    private void DiscardDefenseCard(Card card)
    {
        if (_modeManager.CurrentMode is AttackMode currentMode)
        {
            Logger.Debug($"Discarding defense card to {currentMode.TargetPlayer.PlayerName}'s board", _shouldLog);
            currentMode.TargetPlayer.PlayerBoard.AddCard(card, true);
        }

        ExitCurrentMode();
    }

    private void AttackButtonOnPressed(bool pressed)
    {
        Logger.Debug($"Attack button {(pressed ? "pressed" : "released")}", _shouldLog);
        _modeManager.EnterMode(
            pressed ? CombatModeManager.CombatModeType.Attack : CombatModeManager.CombatModeType.None);
    }

    public void ExecuteAttack(Player target)
    {
        Logger.Debug($"Executing attack on {target.PlayerName}", _shouldLog);
        _attackButton.Disabled = true;
        HasAttacked = true;
        RollForAttack();

        Card drawnCard = DrawDefenseCard(target);
        ResolveAttack(target, drawnCard);
    }

    private Card DrawDefenseCard(Player target)
    {
        Logger.Debug($"{target.PlayerName} drawing defense card", _shouldLog);
        return target.DrawCardForDefense(() =>
        {
            _endTurnButton.Disabled = false;
            if (_combatTurnManager.CurrentPlayer.CurrentCardPlays < _combatTurnManager.CurrentPlayer.TotalCardPlays)
            {
                CombatUiManager.ShowHand(_combatTurnManager.CurrentPlayer, true);
            }
        });
    }

    private void ResolveAttack(Player target, Card defenseCard)
    {
        int defenseValue = CalculateDefenseValue(defenseCard);
        Logger.Debug($"Attack roll: {_rollValue} vs Defense: {defenseValue}", _shouldLog);

        if (defenseValue > _rollValue)
        {
            Logger.Debug($"{target.PlayerName} blocked the attack", _shouldLog);
            target.PlayBlockAnimation();
        }
        else
        {
            Logger.Debug($"{target.PlayerName} was hit by the attack", _shouldLog);
            target.PlayHitAnimation();
        }
    }

    private int CalculateDefenseValue(Card card)
    {
        if (_combatTurnManager.CurrentPlayer.IsAlly)
        {
            return card.NumericValue;
        }

        return card.NumericValue > 11 ? 10 : card.NumericValue;
    }

    private void ExitCurrentMode()
    {
        Logger.Debug("Exiting current combat mode", _shouldLog);
        _modeManager.ExitCurrentMode();
    }

    private void RollForAttack()
    {
        Random random = new();
        int randomNumber = random.Next(1, 11);
        _rollValue = randomNumber + _combatTurnManager.CurrentPlayer.MasteryBonus;

        Logger.Debug($"Attack roll: {randomNumber} + {_combatTurnManager.CurrentPlayer.MasteryBonus} = {_rollValue}",
            _shouldLog);

        _rollIndicator.GetNode<Label>("Label").Text = $"{_rollValue}";
        _rollIndicator.GetNode<AnimationPlayer>("AnimationPlayer").Play("show_result");
    }

    public void NextTurn()
    {
        Player currentPlayer = _combatTurnManager.CurrentPlayer;
        Logger.Debug($"Starting next turn from {currentPlayer.PlayerName}", _shouldLog);

        SetAllPlayerButtonsEnabled(false);
        HandleTurnTransition(currentPlayer);
    }

    private void HandleTurnTransition(Player currentPlayer)
    {
        CleanupCurrentPlayer(currentPlayer);
        Player nextPlayer = PrepareNextPlayer(currentPlayer);
        TransitionToNextPlayer(currentPlayer, nextPlayer);
    }

    private void CleanupCurrentPlayer(Player currentPlayer)
    {
        if (!currentPlayer.IsAlly)
        {
            Logger.Debug($"Removing enemy turn handler from {currentPlayer.PlayerName}", _shouldLog);
            currentPlayer.EnemyFinishedTurn -= OnEnemyFinishedTurn;
        }
    }

    private Player PrepareNextPlayer(Player currentPlayer)
    {
        Player nextPlayer = _combatTurnManager.GetNextPlayer();
        Logger.Debug($"Next player will be: {nextPlayer.PlayerName}", _shouldLog);

        _combatUiManager.HandleCurrentAllyHandExit(currentPlayer, nextPlayer);

        if (!currentPlayer.IsAlly && nextPlayer.IsAlly)
        {
            HandleAllyTransition();
        }

        return nextPlayer;
    }

    private void HandleAllyTransition()
    {
        Player lastAlly = _combatTurnManager.GetLastAlly();
        Logger.Debug($"Hiding last ally hand: {lastAlly.PlayerName}", _shouldLog);
        CombatUiManager.HideLastAllyHand(lastAlly);
    }

    private void TransitionToNextPlayer(Player currentPlayer, Player nextPlayer)
    {
        _combatUiManager.HideLastPlayerBoard(nextPlayer.IsAlly);

        currentPlayer = _combatTurnManager.SwapTurnToNextPlayer(nextPlayer);

        if (!currentPlayer.IsAlly)
        {
            Logger.Debug($"Setting up enemy turn handler for {currentPlayer.PlayerName}", _shouldLog);
            currentPlayer.EnemyFinishedTurn += OnEnemyFinishedTurn;
        }

        _combatUiManager.ShowPlayerUi(currentPlayer);
        HasAttacked = false;
        currentPlayer.StartTurn();
    }

    private void InitEnemyAi()
    {
        Logger.Debug("Initializing enemy AI turn", _shouldLog);
        GetTree().CreateTimer(1).Timeout += _combatTurnManager.CurrentPlayer.EnemyPlayHand;
        _combatTurnManager.CurrentPlayer.EnemyFinishedTurn += OnEnemyFinishedTurn;
    }

    private void OnEnemyFinishedTurn()
    {
        Logger.Debug("Enemy finished turn, executing attack sequence", _shouldLog);
        _modeManager.EnterMode(CombatModeManager.CombatModeType.Attack, false);

        if (_modeManager.CurrentMode is AttackMode currentMode)
        {
            ScheduleEnemyAttack(currentMode);
            ScheduleNextTurn();
        }
    }

    private void ScheduleEnemyAttack(AttackMode currentMode)
    {
        Logger.Debug("Scheduling enemy attack", _shouldLog);
        GetTree().CreateTimer(1.75).Timeout += () => ExecuteAttack(currentMode.TargetPlayer);
    }

    private void ScheduleNextTurn()
    {
        Logger.Debug("Scheduling turn transition", _shouldLog);
        GetTree().CreateTimer(4).Timeout += NextTurn;
    }
}
