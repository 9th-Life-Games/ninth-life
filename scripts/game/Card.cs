using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class Card : Sprite2D
{
    [Signal]
    public delegate void CardLeftTreeEventHandler(Card card);

    private AnimationPlayer _animationPlayer;
    private Button _button;
    private CardState _currentState = CardState.Idle;

    public string CardName;

    public bool IsFaceCard;

    public int NumericValue;

    public CardLibrary.SuitType SuitType;

    public override void _Ready()
    {
        TreeEntered += OnTreeEntered;

        _button = GetNode<Button>("Button");
        _button.MouseEntered += OnHoverIn;
        _button.MouseExited += OnHoverOut;
        _button.Pressed += OnExit;

        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _animationPlayer.AnimationFinished += OnAnimationFinished;
    }

    public void DisableCard()
    {
        _button.Visible = false;
    }

    public void EnableCard()
    {
        _button.Visible = true;
    }

    private void OnTreeEntered()
    {
        _currentState = CardState.Enter;
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
        _currentState = CardState.Exit;
        _button.MouseEntered -= OnHoverIn;
        _button.MouseExited -= OnHoverOut;
        _button.Pressed -= OnExit;
    }

    private void OnAnimationFinished(StringName animationName)
    {
        if (animationName == "exit")
        {
            EmitSignal(SignalName.CardLeftTree, Duplicate());
            QueueFree();
        }

        else if (animationName == "enter")
        {
            _currentState = CardState.Idle;
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