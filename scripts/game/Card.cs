using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class Card : Sprite2D
{
    [Signal]
    public delegate void CardLeftTreeEventHandler(Card card);

    public enum CardMode
    {
        Enabled,
        Disabled,
        Display
    }

    private const float StateTransitionDelay = 0.1f;
    private readonly bool _shouldLog = false;

    private AnimationPlayer _animationPlayer;
    private Button _button;
    private bool _clickDisabled;
    private CardState _currentState = CardState.Idle;
    private bool _isButtonVisible;

    public CardLibrary.SuitType SuitType { get; private set; }
    public int NumericValue { get; private set; }
    public bool IsFaceCard { get; private set; }

    public string GetCardName()
    {
        return $"{SuitType}: {NumericValue}";
    }

    public override void _Ready()
    {
        InitializeComponents();
        SetupEventHandlers();
    }

    private void InitializeComponents()
    {
        Logger.Debug($"Card: Initializing card components for {GetCardName()}", _shouldLog);
        _button ??= GetNode<Button>("EndTurn");
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    private void SetupEventHandlers()
    {
        Logger.Debug($"Card: Setting up event handlers for {GetCardName()}", _shouldLog);
        _button.MouseEntered += OnHoverIn;
        _button.MouseExited += OnHoverOut;
        _button.Pressed += OnExit;
        _animationPlayer.AnimationFinished += OnAnimationFinished;
    }

    public void Initialize(CardLibrary.SuitType suitType, int numericValue, bool isFaceCard)
    {
        Logger.Debug($"Card: Initializing card: Type={suitType}, Value={numericValue}, IsFace={isFaceCard}",
            _shouldLog);
        SuitType = suitType;
        NumericValue = numericValue;
        IsFaceCard = isFaceCard;
    }

    public void SetSuitType(CardLibrary.SuitType suitType)
    {
        SuitType = suitType;
    }

    public void SetNumericValue(int numericValue)
    {
        NumericValue = numericValue;
        IsFaceCard = numericValue > 10;
    }

    public void SetMode(CardMode mode, bool playEnterAnimation = false)
    {
        Logger.Debug($"Card: Setting mode to {mode} for {GetCardName()} (Enter Animation: {playEnterAnimation})",
            _shouldLog);
        _button ??= GetNode<Button>("Button");

        UpdateButtonState(mode);

        if (playEnterAnimation)
        {
            _currentState = CardState.Enter;
        }
    }

    private void UpdateButtonState(CardMode mode)
    {
        switch (mode)
        {
            case CardMode.Enabled:
                Logger.Debug($"Card: Enabling button for {GetCardName()}", _shouldLog);
                _button.Visible = true;
                _clickDisabled = false;
                break;
            case CardMode.Disabled:
                Logger.Debug($"Card: Disabling button for {GetCardName()}", _shouldLog);
                _button.Visible = false;
                _clickDisabled = false;
                break;
            case CardMode.Display:
                Logger.Debug($"Card: Setting display mode for {GetCardName()}", _shouldLog);
                _button.Visible = true;
                _clickDisabled = true;
                break;
        }
    }

    public void DisableCard()
    {
        Logger.Debug($"Card: Disabling card {GetCardName()}", _shouldLog);
        _button.Visible = false;
    }

    public void EnableCard()
    {
        Logger.Debug($"Card: Enabling card {GetCardName()}", _shouldLog);
        _button.Visible = true;
    }

    public override void _Process(double delta)
    {
        UpdateCardState();
    }

    private void UpdateCardState()
    {
        switch (_currentState)
        {
            case CardState.Idle:
                _animationPlayer.Play("idle");
                break;
            case CardState.Enter:
                HandleEnterState();
                break;
            case CardState.Exit:
                _animationPlayer.Play("exit");
                break;
        }
    }

    private void HandleEnterState()
    {
        _isButtonVisible = _button.Visible;
        _animationPlayer.Play("enter");
    }

    private void OnHoverIn()
    {
        Logger.Debug($"Card: Mouse entered {GetCardName()}", _shouldLog);
        _animationPlayer.Play("hover_in");
        _currentState = CardState.Hovered;
    }

    private void OnHoverOut()
    {
        Logger.Debug($"Card: Mouse exited {GetCardName()}", _shouldLog);
        _animationPlayer.Play("hover_out");
        GetTree().CreateTimer(StateTransitionDelay).Timeout += () => _currentState = CardState.Idle;
    }

    private void OnExit()
    {
        if (_clickDisabled)
        {
            Logger.Debug($"Card: Click ignored - card {GetCardName()} is disabled", _shouldLog);
            return;
        }

        Logger.Debug($"Card: Card {GetCardName()} clicked, initiating exit", _shouldLog);
        _currentState = CardState.Exit;
        CleanupEventHandlers();
    }

    public void ExitCard()
    {
        Logger.Debug($"Card: Forcing exit for card {GetCardName()}", _shouldLog);
        _currentState = CardState.Exit;
    }

    private void OnAnimationFinished(StringName animationName)
    {
        Logger.Debug($"Card: Animation {animationName} finished for {GetCardName()}", _shouldLog);
        HandleAnimationComplete(animationName);
    }

    private void HandleAnimationComplete(StringName animationName)
    {
        if (animationName == "exit")
        {
            Logger.Debug($"Card: Exit animation complete, removing {GetCardName()}", _shouldLog);
            EmitSignal(SignalName.CardLeftTree, this.DuplicateCard());
            QueueFree();
        }
        else if (animationName == "enter")
        {
            Logger.Debug($"Card: Enter animation complete for {GetCardName()}", _shouldLog);
            _button.Visible = _isButtonVisible;
            _currentState = CardState.Idle;
        }
    }

    private void CleanupEventHandlers()
    {
        _button.MouseEntered -= OnHoverIn;
        _button.MouseExited -= OnHoverOut;
        _button.Pressed -= OnExit;
    }

    public override void _ExitTree()
    {
        Logger.Debug($"Card: Cleaning up resources for {GetCardName()}", _shouldLog);
        CleanupResources();
    }

    private void CleanupResources()
    {
        if (_button != null)
        {
            SafelyDisconnectButtonEvents();
        }

        if (_animationPlayer != null)
        {
            SafelyDisconnectAnimationEvents();
        }

        Texture = null;
    }

    private void SafelyDisconnectButtonEvents()
    {
        if (_button.IsConnected("mouse_entered", new Callable(this, nameof(OnHoverIn))))
        {
            _button.MouseEntered -= OnHoverIn;
        }

        if (_button.IsConnected("mouse_exited", new Callable(this, nameof(OnHoverOut))))
        {
            _button.MouseExited -= OnHoverOut;
        }

        if (_button.IsConnected("pressed", new Callable(this, nameof(OnExit))))
        {
            _button.Pressed -= OnExit;
        }
    }

    private void SafelyDisconnectAnimationEvents()
    {
        if (_animationPlayer.IsConnected("animation_finished", new Callable(this, nameof(OnAnimationFinished))))
        {
            _animationPlayer.AnimationFinished -= OnAnimationFinished;
        }
    }

    private enum CardState
    {
        Idle,
        Hovered,
        Enter,
        Exit
    }
}
