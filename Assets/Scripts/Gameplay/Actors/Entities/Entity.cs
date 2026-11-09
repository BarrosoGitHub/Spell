using System;
using System.Collections.Generic;
using UnityEngine;

public class Entity
{
    private Stats baseStats;
    public Stats stats;
    private List<Equipment> equipmentList;
    private readonly Dictionary<object, Stats> statModifiers = new Dictionary<object, Stats>();

    public Action<float> OnHealthChanged;
    public Action OnEntityDied;

    private float currentExperience;
    public float LevelUpExperience => stats.Level * 2f;

    private float currentHealth;
    public float CurrentHealth => currentHealth;

    public Entity(Stats baseStats, List<Equipment> equipmentList = null)
    {
        this.baseStats = baseStats;
        this.equipmentList = equipmentList ?? new List<Equipment>();
        UpdateStats();
        currentHealth = stats.MaxHealth;
    }

    public bool AddExperienceAndCheckLevelUp(float amount)
    {
        if (amount <= 0f) return false;

        currentExperience += amount;

        if (currentExperience >= LevelUpExperience)
        {
            currentExperience = 0f;
            baseStats.Level++;
            UpdateStats();
            Debug.Log($"Entity leveled up to level {stats.Level}.");
            return true;
        }
        return false;
    }

    public void ResetLevel()
    {
        currentExperience = 0f;
        baseStats.Level = 1;
        UpdateStats();
    }

    public void AddBaseStats(Stats additionalStats)
    {
        baseStats = baseStats.Add(additionalStats);
        UpdateStats();
    }

    public void Equip(Equipment equipment)
    {
        if (equipment == null) return;

        equipmentList.Add(equipment);
        UpdateStats();
    }

    public void Unequip(Equipment equipment)
    {
        if (equipment == null) return;
        if (!equipmentList.Remove(equipment)) return;

        UpdateStats();
    }

    public void AddStatModifier(object source, Stats modifier)
    {
        statModifiers[source] = modifier;
        UpdateStats();
    }

    public void RemoveStatModifier(object source)
    {
        if (statModifiers.Remove(source)) UpdateStats();
    }

    private void UpdateStats()
    {
        stats = baseStats;

        foreach (var equipment in equipmentList)
        {
            stats = stats.Add(equipment.Stats);
        }

        foreach (Stats modifier in statModifiers.Values)
        {
            stats = stats.Add(modifier);
        }
    }

    public float AddCurrentHealth(float amount)
    {
        float previousHealth = currentHealth;
        float value = currentHealth + amount;

        if (value > stats.MaxHealth)
        {
            currentHealth = stats.MaxHealth;
        }
        else if (value <= 0f)
        {
            currentHealth = 0f;
            if (previousHealth > 0f)
                OnEntityDied?.Invoke();
        }
        else
        {
            currentHealth = value;
        }

        if (previousHealth != currentHealth)
            OnHealthChanged?.Invoke(currentHealth);

        return currentHealth;
    }
}
