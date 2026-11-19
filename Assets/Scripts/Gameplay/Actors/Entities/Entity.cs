using System;
using System.Collections.Generic;
using UnityEngine;

public class Entity
{
    public Stats stats { get; private set; }
    private readonly List<StatModifier> statModifiers = new List<StatModifier>();
    private readonly ControllerStats controllerStats;


    public event Action<float> OnHealthChanged;
    public event Action OnEntityDied;

    public float CurrentHealth { get; private set; }
    public int Level { get; private set; } = 1;
    public float CurrentExperience { get; private set; }
    public float LevelUpExperience => Level * 2f;

    public Entity(ControllerStats controllerStats = null)
    {
        if (controllerStats == null)
            throw new ArgumentNullException(nameof(controllerStats), "Assign a ControllerStats asset before creating an Entity.");

        this.controllerStats = controllerStats;
    
        UpdateStats();
        RestoreHealth();
    }
    public float AddHealth(float amount)
    {
        float previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, Mathf.Max(0f, stats.MaxHealth));
        if (CurrentHealth <= 0f && previousHealth > 0f)
            OnEntityDied?.Invoke();
        if (previousHealth != CurrentHealth)
            OnHealthChanged?.Invoke(CurrentHealth);
        return CurrentHealth;
    }

    public void RestoreHealth() => AddHealth(stats.MaxHealth - CurrentHealth);

    public bool AddExperience(float amount)
    {
        if (amount <= 0f) return false;
        CurrentExperience += amount;
        if (CurrentExperience < LevelUpExperience) return false;
        SetLevel(Level + 1);
        return true;
    }

    public void SetLevel(int level)
    {
        Level = Mathf.Max(1, level);
        CurrentExperience = 0f;
        UpdateStats();
    }

    public void ResetLevel() => SetLevel(1);

    

    public void AddStatModifier(StatModifier modifier)
    {
        if (modifier == null) return;
        if (!statModifiers.Contains(modifier))
            statModifiers.Add(modifier);
        UpdateStats();
    }

    public void RemoveStatModifier(StatModifier modifier)
    {
        if (statModifiers.Remove(modifier)) UpdateStats();
    }

    private void UpdateStats()
    {
        stats = controllerStats.CreateBaseStats().Add(controllerStats.GetBonus(Level));

        foreach (StatModifier modifier in statModifiers)
        {
            stats = stats.Add(modifier.Stats);
        }
    }
}
