using System;
using System.Collections.Generic;
using UnityEngine;

public class Entity
{
    private Stats baseStats;
    public Stats stats;
    private List<Equipment> equipmentList;

    public Action<float> OnHealthChanged;
    public Action OnEntityDied;

    public event Action OnLevelUp;

    private float currentExperience;
    public float CurrentExperience => currentExperience;

    public float LevelUpExperience => stats.Level * 2f;

    private float currentHealth;
    public float CurrentHealth
    {
        get
        {
            return currentHealth;
        }
        set
        {
            float previousHealth = currentHealth;

            if (value > stats.MaxHealth)
            {
                currentHealth = stats.MaxHealth;
            }
            else if (value <= 0)
            {
                currentHealth = 0;
                OnEntityDied?.Invoke();
            }
            else
            {
                currentHealth = value;
            }

            if (previousHealth != currentHealth)
            {
                OnHealthChanged?.Invoke(currentHealth);
            }
        }
    }

    public Entity(Stats baseStats, List<Equipment> equipmentList = null)
    {
        this.baseStats = baseStats;
        this.equipmentList = equipmentList ?? new List<Equipment>();
        UpdateStats();
        currentHealth = stats.MaxHealth;
    }

    public void AddExperience(float amount)
    {
        if (amount <= 0f) return;

        currentExperience += amount;

        if (currentExperience >= LevelUpExperience)
        {
            currentExperience = 0f;
            baseStats.Level++;
            UpdateStats();
            Debug.Log($"Entity leveled up to level {stats.Level}.");
            OnLevelUp?.Invoke();
        }
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

    private void UpdateStats()
    {
        stats = baseStats;

        foreach (var equipment in equipmentList)
        {
            stats = stats.Add(equipment.Stats);
        }
    }

    public float TakeDamage(float amount)
    {
        CurrentHealth -= amount;
        return amount;
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
    }
}
