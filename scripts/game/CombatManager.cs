using System.Linq;
using Godot;
using NinthLife.scripts.game.combat;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class CombatManager : Node2D
{
    private Button _attackButton;
    private CombatUiManager _combatUiManager;
    private Node2D _defenseDraw;
    private Button _endTurnButton;
    private int _previewIndex = -1;
    private Node2D _rollIndicator;
    private TurnOrderDisplay _turnOrderDisplay;
    public TurnManager TurnManager { get; private set; }

    public override void _Input(InputEvent @event)
    {
        if (!IsCurrentPlayerAlly())
        {
            return;
        }

        if (TurnManager.IsInAttackMode)
        {
            HandleAttackModeInput(@event);
        }
        else
        {
            HandleNormalInput(@event);
        }
    }

    private bool IsCurrentPlayerAlly()
    {
        return TurnManager?.CurrentPlayer is { IsAlly: true };
    }

    private void HandleAttackModeInput(InputEvent @event)
    {
        if (Input.IsActionPressed("accept"))
        {
            TurnManager.OnCombatTargetSelected();
        }
        else if (Input.IsActionJustPressed("previous_preview"))
        {
            if (TurnManager.CurrentState is CombatPhaseState combatState)
            {
                combatState.CycleTargetBackward();
            }
        }
        else if (Input.IsActionJustPressed("next_preview"))
        {
            if (TurnManager.CurrentState is CombatPhaseState combatState)
            {
                combatState.CycleTargetForward();
            }
        }
    }

    private void HandleNormalInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed("previous_preview"))
        {
            CyclePreviewBackward();
        }
        else if (Input.IsActionJustPressed("next_preview"))
        {
            CyclePreviewForward();
        }
        else if (Input.IsActionJustPressed("next_turn") &&
                 TurnManager.CurrentPlayer.IsHandEnabled)
        {
            TurnManager.TransitionTo<EndTurnState>();
        }
    }

    public override void _Ready()
    {
        InitializeComponents();
        InitializeManagers();
        SetupEventHandlers();
    }

    private void InitializeComponents()
    {
        _turnOrderDisplay = GetNode<TurnOrderDisplay>("../TurnOrderDisplay");
        _endTurnButton = GetNode<Button>("../EndTurn");
        _rollIndicator = GetNode<Node2D>("../RollIndicator");
        _attackButton = GetNode<Button>("../Attack");
        _defenseDraw = GetNode<Node2D>("../DefenseDraw");
    }

    private void InitializeManagers()
    {
        _combatUiManager = new CombatUiManager();

        // Create the TurnManager but don't start the first state yet
        TurnManager = new TurnManager(
            null,
            _combatUiManager,
            _turnOrderDisplay,
            _rollIndicator,
            _defenseDraw,
            false // Add this parameter to constructor: don't auto-start
        );

        _combatUiManager.InitUiManager(_endTurnButton, _attackButton, TurnManager);
        _combatUiManager.InitPlayerUi(TurnManager.CurrentPlayer, TurnManager.FirstAlly, TurnManager.FirstEnemy);

        // Initialize players first
        TurnManager.InitializePlayers(this);
        TurnManager.SetInitialAlliesAndEnemies(GetChildren().OfType<Player>());

        // Now that everything is set up, we can start the first state
        TurnManager.StartFirstTurn();
    }

    private void SetupEventHandlers()
    {
        _defenseDraw.ChildEnteredTree += OnDefenseCardEntered;
        _attackButton.Toggled += OnAttackButtonToggled;
    }

    private void OnDefenseCardEntered(Node node)
    {
        if (node is Card card)
        {
            card.CardLeftTree += OnDefenseCardLeft;
        }
    }

    private void OnDefenseCardLeft(Card card)
    {
        if (TurnManager.CurrentState is CombatPhaseState combatState)
        {
            // Add the defense card to the current target's board
            // We'll need to add a method to get the current target from CombatPhaseState
            Player target = combatState.CurrentTarget;
            if (target != null)
            {
                target.PlayerBoard.AddCard(card, true);
            }
        }
    }

    private void OnAttackButtonToggled(bool pressed)
    {
        if (pressed)
        {
            TurnManager.TransitionTo<CombatPhaseState>();
        }
        else if (TurnManager.CurrentState is CombatPhaseState)
        {
            TurnManager.TransitionTo<MainPhaseState>();
        }
    }

    private void CyclePreviewForward()
    {
        if (_previewIndex == -1)
        {
            _previewIndex = TurnManager.GetCurrentPlayerIndex();
        }

        _previewIndex = (_previewIndex + 1) % TurnManager.GetTurnOrderCount();
        Player playerToPreview = TurnManager.GetPlayerToPreview(_previewIndex);
        _combatUiManager.StartPreview(playerToPreview, TurnManager.CurrentPlayer);
    }

    private void CyclePreviewBackward()
    {
        if (_previewIndex == -1)
        {
            _previewIndex = TurnManager.GetCurrentPlayerIndex();
        }

        _previewIndex--;
        if (_previewIndex < 0)
        {
            _previewIndex = TurnManager.GetTurnOrderCount() - 1;
        }

        Player playerToPreview = TurnManager.GetPlayerToPreview(_previewIndex);
        _combatUiManager.StartPreview(playerToPreview, TurnManager.CurrentPlayer);
    }

    public override void _Process(double delta)
    {
        TurnManager?.Update();
    }
}
