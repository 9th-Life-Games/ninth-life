using Godot;
using NinthLife.scripts.components;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.game;

public partial class HealthBar : Control
{
    private readonly bool _shouldLog = true;
    private ProgressBar _healthBar;

    private Health _healthComponent;
    // private Label _healthLabel;

    public override void _Ready()
    {
        Logger.Debug("HealthBar: Initializing health bar", _shouldLog);
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        _healthBar = GetNode<ProgressBar>("ProgressBar");
        // _healthLabel = GetNode<Label>("HealthLabel");
    }

    public void Initialize(Health healthComponent)
    {
        Logger.Debug("HealthBar: Setting up health component connection", _shouldLog);
        _healthComponent = healthComponent;
        _healthComponent.HealthChanged += UpdateHealthDisplay;
        _healthComponent.DamageTaken += OnDamageTaken;
        _healthComponent.Healed += OnHealed;
    }

    private void UpdateHealthDisplay(int currentHealth, int maxHealth)
    {
        Logger.Debug($"HealthBar: Updating display - Current: {currentHealth}, Max: {maxHealth}", _shouldLog);
        _healthBar.MaxValue = maxHealth;
        _healthBar.Value = currentHealth;
        // _healthLabel.Text = $"{currentHealth}/{maxHealth}";
    }

    private void OnDamageTaken(int amount)
    {
        Logger.Debug($"HealthBar: Damage taken animation for {amount} damage", _shouldLog);
        // You could add animations or effects here when damage is taken
        _healthBar.Modulate = Colors.Red;
        GetTree().CreateTimer(0.2f).Timeout += () => _healthBar.Modulate = Colors.White;
    }

    private void OnHealed(int amount)
    {
        Logger.Debug($"HealthBar: Heal animation for {amount} healing", _shouldLog);
        // You could add animations or effects here when healed
        _healthBar.Modulate = Colors.Green;
        GetTree().CreateTimer(0.2f).Timeout += () => _healthBar.Modulate = Colors.White;
    }
}
