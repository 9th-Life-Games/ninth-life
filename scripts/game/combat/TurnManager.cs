using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game.combat;

public class TurnManager
{
    private readonly bool _autoStart;
    private readonly Node2D _defenseDraw;
    private readonly Node2D _rollIndicator;
    private readonly Dictionary<Type, ICombatState> _states;
    private readonly List<Player> _turnOrder = new();
    private readonly TurnOrderDisplay _turnOrderDisplay;
    private readonly CombatUiManager _uiManager;

    public readonly List<Player> AllyTurnOrder = new();
    public readonly List<Player> EnemyTurnOrder = new();

    public TurnManager(
        Player currentPlayer,
        CombatUiManager uiManager,
        TurnOrderDisplay turnOrderDisplay,
        Node2D rollIndicator,
        Node2D defenseDraw,
        bool autoStart = true)
    {
        CurrentPlayer = currentPlayer;
        _turnOrderDisplay = turnOrderDisplay;
        _uiManager = uiManager;
        _rollIndicator = rollIndicator;
        _defenseDraw = defenseDraw;
        _autoStart = autoStart;

        _states = CreateStates();
    }

    public ICombatState CurrentState { get; private set; }

    public Player FirstAlly { get; private set; }
    public Player FirstEnemy { get; private set; }
    public Player CurrentPlayer { get; private set; }

    public bool IsInCombatPhase => CurrentState is CombatPhaseState;
    public bool IsInAttackMode => (CurrentState as CombatPhaseState)?.IsInAttackMode ?? false;

    private Dictionary<Type, ICombatState> CreateStates()
    {
        return new Dictionary<Type, ICombatState>
        {
            { typeof(StartTurnState), CreateState(typeof(StartTurnState)) },
            { typeof(DrawPhaseState), CreateState(typeof(DrawPhaseState)) },
            { typeof(MainPhaseState), CreateState(typeof(MainPhaseState)) },
            { typeof(CombatPhaseState), CreateCombatState() },
            { typeof(EndTurnState), CreateState(typeof(EndTurnState)) }
        };
    }

    private ICombatState CreateState(Type type)
    {
        // Create instance with constructor params: TurnManager, Player, CombatUiManager
        return (ICombatState)Activator.CreateInstance(
            type,
            this, // TurnManager
            CurrentPlayer, // Player
            _uiManager // CombatUiManager
        );
    }

    private CombatPhaseState CreateCombatState()
    {
        // CombatPhaseState has extra parameters
        return new CombatPhaseState(
            this, // TurnManager
            CurrentPlayer, // Player
            _uiManager, // CombatUiManager
            _rollIndicator, // Node2D rollIndicator
            _defenseDraw // Node2D defenseDraw
        );
    }

    private void RecreateStates()
    {
        Dictionary<Type, ICombatState> newStates = CreateStates();

        // If we're in a state, find the equivalent new state
        if (CurrentState != null)
        {
            Type currentStateType = CurrentState.GetType();
            CurrentState.Exit();
            CurrentState = newStates[currentStateType];
            CurrentState.Enter();
        }

        _states.Clear();
        foreach (KeyValuePair<Type, ICombatState> kvp in newStates)
        {
            _states[kvp.Key] = kvp.Value;
        }
    }

    public void InitializePlayers(CombatManager combatManager)
    {
        foreach (Player player in GameUtils.CombatEntities.OrderByDescending(static player => player.Initiative))
        {
            CurrentPlayer ??= player;
            _turnOrder.Add(player);
            _turnOrderDisplay.AddAvatar(player);

            if (player.IsAlly)
            {
                AllyTurnOrder.Add(player);
            }
            else
            {
                EnemyTurnOrder.Add(player);
            }

            combatManager.AddChild(player);
        }

        CurrentPlayer.TurnIndicator.Visible = true;

        // Only auto-start if configured to do so
        if (_autoStart)
        {
            StartFirstTurn();
        }
    }

    public void StartFirstTurn()
    {
        // Create fresh states with current player
        RecreateStates();
        // Start the first turn
        TransitionTo<StartTurnState>();
    }

    public void SetInitialAlliesAndEnemies(IEnumerable<Player> players)
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

    public Player GetNextPlayer()
    {
        return _turnOrder[1];
    }

    public int GetCurrentPlayerIndex()
    {
        return _turnOrder.IndexOf(CurrentPlayer);
    }

    public int GetTurnOrderCount()
    {
        return _turnOrder.Count;
    }

    public Player GetPlayerToPreview(int index)
    {
        return _turnOrder[index];
    }

    public Player GetLastAlly()
    {
        return _turnOrder.FindLast(player => player.IsAlly);
    }

    public void TransitionTo<T>() where T : ICombatState
    {
        ICombatState nextState = _states[typeof(T)];

        if (CurrentState?.CanTransitionTo(nextState) == false)
        {
            return;
        }

        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }

    public void Update()
    {
        CurrentState?.Update();
    }

    public Player SwapTurnToNextPlayer(Player nextPlayer)
    {
        // End current player's turn
        CurrentPlayer.EndTurn();

        // Update turn order
        _turnOrderDisplay.CycleAvatars(CurrentPlayer);
        _turnOrder.Remove(CurrentPlayer);
        _turnOrder.Add(CurrentPlayer);

        // Update current player
        CurrentPlayer = nextPlayer;

        return CurrentPlayer;
    }

    public void OnCombatTargetSelected()
    {
        if (CurrentState is CombatPhaseState combatState)
        {
            // TODO
            combatState.ConfirmAttack();
        }
    }
}
