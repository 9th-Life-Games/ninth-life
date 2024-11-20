using System;
using Godot;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.components;

public partial class Health : Node
{
    [Signal]
    public delegate void DamageTakenEventHandler(int amount);

    [Signal]
    public delegate void DidUnitDieEventHandler(bool didDie);

    [Signal]
    public delegate void HealedEventHandler(int amount);

    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

    [Signal]
    public delegate void UnitDiedEventHandler();

    private readonly bool _shouldLog = true;
    private int _currentHealth;
    private int _maxHealth;

    public int CurrentHealth
    {
        get => _currentHealth;
        private set
        {
            _currentHealth = Math.Clamp(value, 0, _maxHealth);
            EmitSignal(SignalName.HealthChanged, _currentHealth, _maxHealth);

            if (_currentHealth <= 0)
            {
                EmitSignal(SignalName.UnitDied);
            }
        }
    }

    public int MaxHealth
    {
        get => _maxHealth;
        private set
        {
            _maxHealth = value;
            CurrentHealth = Math.Min(CurrentHealth, _maxHealth);
        }
    }

    public void Initialize(int maxHealth)
    {
        Logger.Debug($"Health: Initializing with max health: {maxHealth}", _shouldLog);
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(string weaponType)
    {
        int calculatedDamage = CalculateDamage(weaponType);
        Logger.Debug($"Health: Taking {calculatedDamage} damage from {weaponType}", _shouldLog);

        CurrentHealth -= calculatedDamage;
        EmitSignal(SignalName.DamageTaken, calculatedDamage);
        EmitSignal(SignalName.DidUnitDie, CurrentHealth <= 0);
    }

    private int CalculateDamage(string weaponType)
    {
        // Base damage values for different weapon types
        return weaponType switch
        {
            "longblade" => 5,
            "shortblade" => 5,
            "mace" => 5,
            _ => 3
        };
    }

    public void Heal(int amount)
    {
        Logger.Debug($"Health: Healing for {amount}", _shouldLog);
        int healAmount = Math.Min(amount, MaxHealth - CurrentHealth);
        CurrentHealth += healAmount;
        EmitSignal(SignalName.Healed, healAmount);
    }

    public float GetHealthPercentage()
    {
        return (float)CurrentHealth / MaxHealth;
    }
}
