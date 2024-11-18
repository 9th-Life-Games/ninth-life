using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.game.combat;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class CombatManager : Node2D
{
    [Signal]
    public delegate void AttackModeFinishedEventHandler();

    [Signal]
    public delegate void EnemyTurnResolvedEventHandler();

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
            Logger.Debug("CombatManager: Next turn input received from enabled ally", _shouldLog);
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
            Logger.Debug("CombatManager: Entering preview mode (backward)", _shouldLog);
            _modeManager.EnterMode(CombatModeManager.CombatModeType.Preview, false);
        }
        else if (Input.IsActionPressed("next_preview"))
        {
            Logger.Debug("CombatManager: Entering preview mode (forward)", _shouldLog);
            _modeManager.EnterMode(CombatModeManager.CombatModeType.Preview);
        }
    }

    private void InitializeNodes()
    {
        Logger.Debug("CombatManager: Initializing combat manager nodes", _shouldLog);
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
        Logger.Debug("CombatManager: Initializing combat components", _shouldLog);
        _combatTurnManager.InitializePlayers(this, _turnOrderDisplay);
        SetInitialAlliesAndEnemies();

        Logger.Debug("CombatManager: Initializing UI for first turn", _shouldLog);
        _combatUiManager.InitPlayerUi(
            _combatTurnManager.CurrentPlayer,
            FirstAlly,
            FirstEnemy
        );

        _modeManager = new CombatModeManager(this, _combatUiManager, _combatTurnManager);

        if (!_combatTurnManager.CurrentPlayer.IsAlly)
        {
            Logger.Debug("CombatManager: Initializing enemy AI", _shouldLog);
            InitEnemyAi();
        }
        else
        {
            Logger.Debug("CombatManager: Enabling player buttons for ally turn", _shouldLog);
            SetAllPlayerButtonsEnabled(true);
            _combatUiManager.SetEndTurnButtonState(true);
            _combatUiManager.SetAttackButtonState(true);
        }
    }

    private void SetupEventHandlers()
    {
        Logger.Debug("CombatManager: Setting up event handlers", _shouldLog);
        _defenseDraw.ChildEnteredTree += OnDefenseDrawChildEnteredTree;
        _attackButton.Toggled += AttackButtonOnPressed;
    }

    private void SetInitialAlliesAndEnemies()
    {
        Logger.Debug("CombatManager: Setting up initial allies and enemies", _shouldLog);
        _combatTurnManager.TurnOrder.ForEach(player =>
        {
            Logger.Debug($"CombatManager: Setting up player: {player.PlayerName}", _shouldLog);
            player.PlayerClicked += OnPlayerClicked;
            player.AllyTurnReady += OnAllyHandEnabled;

            if (player.IsAlly)
            {
                if (FirstAlly == null)
                {
                    Logger.Debug($"CombatManager: Set first ally: {player.PlayerName}", _shouldLog);
                    FirstAlly = player;
                }
            }
            else
            {
                if (FirstEnemy == null)
                {
                    Logger.Debug($"CombatManager: Set first enemy: {player.PlayerName}", _shouldLog);
                    FirstEnemy = player;
                }
            }
        });

        SetAllPlayerButtonsEnabled(false);
    }

    private void OnPlayerDeathAnimationsCompleted(Player player)
    {
        // Handle any UI updates that should happen after animations but before final cleanup
        Logger.Debug($"CombatManager: Death animations completed for {player.PlayerName}", _shouldLog);

        Player replacementPlayer = GetReplacementPlayer(player);
        _combatUiManager.HandlePlayerDeath(player, replacementPlayer);
    }

    private Player GetReplacementPlayer(Player dyingPlayer)
    {
        if (dyingPlayer.IsAlly)
        {
            return dyingPlayer == _combatTurnManager.AllyTurnOrder[0]
                ? _combatTurnManager.AllyTurnOrder[^1]
                : _combatTurnManager.AllyTurnOrder[0];
        }

        return dyingPlayer == _combatTurnManager.EnemyTurnOrder[0]
            ? _combatTurnManager.EnemyTurnOrder[^1]
            : _combatTurnManager.EnemyTurnOrder[0];
    }

    private void OnAllyHandEnabled()
    {
        Logger.Debug("CombatManager: Ally hand enabled, enabling player buttons", _shouldLog);
        SetAllPlayerButtonsEnabled(true);
    }

    private void SetAllPlayerButtonsEnabled(bool enabled)
    {
        _combatTurnManager.TurnOrder.ForEach(player =>
        {
            Logger.Debug($"CombatManager: Setting {player.PlayerName}'s button enabled: {enabled}", _shouldLog);
            player.SetButtonEnabled(enabled);
        });
    }

    private void OnPlayerClicked(Player player)
    {
        if (_modeManager.CurrentMode is not AttackMode && _modeManager.CurrentMode is not PreviewMode)
        {
            Logger.Debug($"CombatManager: Player clicked: {player.PlayerName}, entering preview mode", _shouldLog);
            _modeManager.EnterMode(CombatModeManager.CombatModeType.Preview, false, player);
        }
    }

    private void OnDefenseDrawChildEnteredTree(Node node)
    {
        Logger.Debug("CombatManager: Defense draw card entered tree, setting up discard handler", _shouldLog);
        ((Card)node).CardLeftTree += DiscardDefenseCard;
    }

    private void DiscardDefenseCard(Card card)
    {
        if (_modeManager.CurrentMode is AttackMode currentMode)
        {
            Logger.Debug($"CombatManager: Discarding defense card to {currentMode.TargetPlayer.PlayerName}'s board",
                _shouldLog);
            currentMode.TargetPlayer.PlayerBoard.AddCard(card, true);
        }

        GetTree().CreateTimer(.5).Timeout += ExitAttackMode;
    }

    private void AttackButtonOnPressed(bool pressed)
    {
        Logger.Debug($"CombatManager: Attack button {(pressed ? "pressed" : "released")}", _shouldLog);
        _modeManager.EnterMode(
            pressed ? CombatModeManager.CombatModeType.Attack : CombatModeManager.CombatModeType.None);
    }

    public void ExecuteAttack(Player target)
    {
        Logger.Debug($"CombatManager: Executing attack on {target.PlayerName}", _shouldLog);
        _attackButton.Disabled = true;
        HasAttacked = true;
        RollForAttack();

        Card drawnCard = DrawDefenseCard(target);
        ResolveAttack(target, drawnCard);
    }

    private Card DrawDefenseCard(Player target)
    {
        Logger.Debug($"CombatManager: {target.PlayerName} drawing defense card", _shouldLog);
        return target.DrawCardForDefense(() =>
        {
            if (_combatTurnManager.CurrentPlayer.CurrentCardPlays < _combatTurnManager.CurrentPlayer.TotalCardPlays)
            {
                CombatUiManager.ShowHand(_combatTurnManager.CurrentPlayer, true);
            }
        });
    }

    private void ResolveAttack(Player target, Card defenseCard)
    {
        int defenseValue = CalculateDefenseValue(defenseCard);
        Logger.Debug($"CombatManager: Attack roll: {_rollValue} vs Defense: {defenseValue}", _shouldLog);

        void OnDidUnitDie(bool didDie)
        {
            void OnAttackFinished()
            {
                AttackModeFinished -= OnAttackFinished;
                if (didDie)
                {
                    RemovePlayerFromGame(target);
                }
                else
                {
                    EmitSignal(SignalName.EnemyTurnResolved);
                }

                if (!target.IsAlly)
                {
                    _combatUiManager.SetEndTurnButtonState(true);
                }
            }

            target.Health.DidUnitDie -= OnDidUnitDie;
            AttackModeFinished += OnAttackFinished;
        }

        target.Health.DidUnitDie += OnDidUnitDie;


        if (defenseValue > _rollValue)
        {
            Logger.Debug($"CombatManager: {target.PlayerName} blocked the attack", _shouldLog);
            target.PlayBlockAnimation();

            void OnAttackFinished()
            {
                AttackModeFinished -= OnAttackFinished;
                if (!target.IsAlly)
                {
                    _combatUiManager.SetEndTurnButtonState(true);
                }

                EmitSignal(SignalName.EnemyTurnResolved);
            }

            AttackModeFinished += OnAttackFinished;
        }
        else
        {
            Logger.Debug($"CombatManager: {target.PlayerName} was hit by the attack", _shouldLog);


            target.PlayHitAnimation();
            target.Health.TakeDamage(_combatTurnManager.CurrentPlayer.WeaponType);
        }
    }

    private int CalculateDefenseValue(Card card)
    {
        if (_combatTurnManager.CurrentPlayer.IsAlly)
        {
            return card.NumericValue > 11 ? 10 : card.NumericValue;
        }

        return card.NumericValue;
    }

    private void RemovePlayerFromGame(Player player)
    {
        int CountAlivePlayers(List<Player> players)
        {
            return players.Count(combatant => !combatant.IsDead);
        }

        switch (player.IsAlly)
        {
            case true when CountAlivePlayers(_combatTurnManager.AllyTurnOrder) == 0:
            {
                Logger.Debug("*****Game Over*****");
                Control endGameScene = ResourceManager.Load<PackedScene>("res://scenes/end_game_display.tscn")
                    .Instantiate<Control>();
                Label label = endGameScene.GetNode<Label>("Label");
                label.Text = "Game Over";
                GetTree().Root.AddChild(endGameScene);
                return;
            }
            case false when CountAlivePlayers(_combatTurnManager.EnemyTurnOrder) == 0:
            {
                Logger.Debug("*****You Win!!!*****");
                Control endGameScene = ResourceManager.Load<PackedScene>("res://scenes/end_game_display.tscn")
                    .Instantiate<Control>();
                Label label = endGameScene.GetNode<Label>("Label");
                label.Text = "You Win!!!";
                GetTree().Root.AddChild(endGameScene);
                return;
            }
            case true when player == _combatUiManager.LastAlly:
            {
                Player replacementPlayer = player == _combatTurnManager.AllyTurnOrder[0]
                    ? _combatTurnManager.AllyTurnOrder[^1]
                    : _combatTurnManager.AllyTurnOrder[0];
                _combatUiManager.HandlePlayerDeath(player, replacementPlayer);
                break;
            }
            case true:
                CombatUiManager.ShowHand(player, false);
                CombatUiManager.ShowBoard(player, false);
                break;
            case false:
            {
                Player replacementPlayer = player == _combatTurnManager.EnemyTurnOrder[0]
                    ? _combatTurnManager.EnemyTurnOrder[^1]
                    : _combatTurnManager.EnemyTurnOrder[0];
                _combatUiManager.HandlePlayerDeath(player, replacementPlayer);
                break;
            }
        }

        player.PlayerDeath(() =>
        {
            _combatTurnManager.RemovePlayer(player);
            GetTree().CreateTimer(0.3).Timeout += () =>
            {
                EmitSignal(SignalName.EnemyTurnResolved);
            };
        });
    }

    private void ExitAttackMode()
    {
        Logger.Debug("CombatManager: Exiting current combat mode", _shouldLog);
        _modeManager.ExitCurrentMode();
        EmitSignal(SignalName.AttackModeFinished);
    }

    private void RollForAttack()
    {
        Random random = new();
        int randomNumber = random.Next(1, 11);
        _rollValue = randomNumber + _combatTurnManager.CurrentPlayer.MasteryBonus;

        Logger.Debug(
            $"CombatManager: Attack roll: {randomNumber} + {_combatTurnManager.CurrentPlayer.MasteryBonus} = {_rollValue}",
            _shouldLog);

        _rollIndicator.GetNode<Label>("Label").Text = $"{_rollValue}";
        _rollIndicator.GetNode<AnimationPlayer>("AnimationPlayer").Play("show_result");
    }

    public void NextTurn()
    {
        Player currentPlayer = _combatTurnManager.CurrentPlayer;
        Logger.Debug($"CombatManager: Starting next turn from {currentPlayer.PlayerName}", _shouldLog);

        SetAllPlayerButtonsEnabled(false);
        HandleTurnTransition(currentPlayer);
    }

    private void HandleTurnTransition(Player currentPlayer)
    {
        CleanupCurrentPlayer(currentPlayer);
        Player nextPlayer = PrepareNextPlayer(currentPlayer);
        if (nextPlayer.IsAlly)
        {
            _combatUiManager.SetEndTurnButtonState(true);
        }

        TransitionToNextPlayer(nextPlayer);
    }

    private void CleanupCurrentPlayer(Player currentPlayer)
    {
        if (!currentPlayer.IsAlly)
        {
            Logger.Debug($"CombatManager: Removing enemy turn handler from {currentPlayer.PlayerName}", _shouldLog);
            currentPlayer.EnemyFinishedPlayingHand -= OnEnemyFinishedPlayingHand;
        }
    }

    private Player PrepareNextPlayer(Player currentPlayer)
    {
        Player nextPlayer = _combatTurnManager.GetNextPlayer();
        Logger.Debug($"CombatManager: Next player will be: {nextPlayer.PlayerName}", _shouldLog);

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
        Logger.Debug($"CombatManager: Hiding last ally hand: {lastAlly.PlayerName}", _shouldLog);
        CombatUiManager.HideLastAllyHand(lastAlly);
    }

    private void TransitionToNextPlayer(Player nextPlayer)
    {
        _combatUiManager.HideLastPlayerBoard(nextPlayer.IsAlly);

        Player currentPlayer = _combatTurnManager.SwapTurnToNextPlayer(nextPlayer);

        if (!currentPlayer.IsAlly)
        {
            Logger.Debug($"CombatManager: Setting up enemy turn handler for {currentPlayer.PlayerName}", _shouldLog);
            currentPlayer.EnemyFinishedPlayingHand += OnEnemyFinishedPlayingHand;
        }

        _combatUiManager.ShowPlayerUi(currentPlayer);
        HasAttacked = false;
        currentPlayer.StartTurn();
    }

    private void InitEnemyAi()
    {
        Logger.Debug("CombatManager: Initializing enemy AI turn", _shouldLog);
        GetTree().CreateTimer(1).Timeout += _combatTurnManager.CurrentPlayer.EnemyPlayHand;
        _combatTurnManager.CurrentPlayer.EnemyFinishedPlayingHand += OnEnemyFinishedPlayingHand;
    }

    private void OnEnemyFinishedPlayingHand()
    {
        Logger.Debug("CombatManager: Enemy finished turn, executing attack sequence", _shouldLog);
        _modeManager.EnterMode(CombatModeManager.CombatModeType.Attack, false);

        if (_modeManager.CurrentMode is AttackMode currentMode)
        {
            ScheduleEnemyAttack(currentMode);
            ScheduleNextTurn();
        }
    }

    private void ScheduleEnemyAttack(AttackMode currentMode)
    {
        Logger.Debug("CombatManager: Scheduling enemy attack", _shouldLog);
        GetTree().CreateTimer(1.75).Timeout += () => ExecuteAttack(currentMode.TargetPlayer);
    }

    private void ScheduleNextTurn()
    {
        Logger.Debug("CombatManager: Scheduling turn transition", _shouldLog);

        void OnEnemyTurnResolved()
        {
            EnemyTurnResolved -= OnEnemyTurnResolved;
            GetTree().CreateTimer(0.5).Timeout += NextTurn;
        }

        EnemyTurnResolved += OnEnemyTurnResolved;
    }
}
