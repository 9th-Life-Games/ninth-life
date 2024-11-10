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


    private AnimationPlayer _animationPlayer;
    private Button _button;
    private bool _clickDisabled;
    private CardState _currentState = CardState.Idle;
    private bool _isButtonVisible;

    public string CardName => $"{SuitType}: {NumericValue}";
    public CardLibrary.SuitType SuitType { get; private set; }
    public int NumericValue { get; private set; }
    public bool IsFaceCard { get; private set; }

    public override void _Ready()
    {
        _button ??= GetNode<Button>("EndTurn");
        _button.MouseEntered += OnHoverIn;
        _button.MouseExited += OnHoverOut;
        _button.Pressed += OnExit;

        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _animationPlayer.AnimationFinished += OnAnimationFinished;
    }

    public void Initialize(CardLibrary.SuitType suitType, int numericValue, bool isFaceCard)
    {
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
        _button ??= GetNode<Button>("Button");

        switch (mode)
        {
            case CardMode.Enabled:
                _button.Visible = true;
                _clickDisabled = false;
                break;
            case CardMode.Disabled:
                _button.Visible = false;
                _clickDisabled = false;
                break;
            case CardMode.Display:
                _button.Visible = true;
                _clickDisabled = true;
                break;
        }

        if (playEnterAnimation)
        {
            _currentState = CardState.Enter;
        }
    }

    public void DisableCard()
    {
        _button.Visible = false;
    }

    public void EnableCard()
    {
        _button.Visible = true;
    }

    public override void _Process(double delta)
    {
        switch (_currentState)
        {
            case CardState.Idle:
                _animationPlayer.Play("idle");
                break;
            case CardState.Hovered:
                break;
            case CardState.Enter:
                _isButtonVisible = _button.Visible;
                _animationPlayer.Play("enter");
                break;
            case CardState.Exit:
                _animationPlayer.Play("exit");
                break;
        }
    }

    private void OnHoverIn()
    {
        _animationPlayer.Play("hover_in");
        _currentState = CardState.Hovered;
    }

    private void OnHoverOut()
    {
        _animationPlayer.Play("hover_out");
        GetTree().CreateTimer(.1).Timeout += () => _currentState = CardState.Idle;
    }

    private void OnExit()
    {
        if (!_clickDisabled)
        {
            _currentState = CardState.Exit;
            _button.MouseEntered -= OnHoverIn;
            _button.MouseExited -= OnHoverOut;
            _button.Pressed -= OnExit;
        }
    }

    public void ExitCard()
    {
        _currentState = CardState.Exit;
    }

    private void OnAnimationFinished(StringName animationName)
    {
        if (animationName == "exit")
        {
            EmitSignal(SignalName.CardLeftTree, this.DuplicateCard());
            QueueFree();
        }
        else if (animationName == "enter")
        {
            _button.Visible = _isButtonVisible;
            _currentState = CardState.Idle;
        }
    }

    public override void _ExitTree()
    {
        // Check if signals are connected before disconnecting
        if (_button != null)
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

        if (_animationPlayer != null &&
            _animationPlayer.IsConnected("animation_finished", new Callable(this, nameof(OnAnimationFinished))))
        {
            _animationPlayer.AnimationFinished -= OnAnimationFinished;
        }

        // Clear resources
        Texture = null;
    }

    private enum CardState
    {
        Idle,
        Hovered,
        Enter,
        Exit
    }
}
