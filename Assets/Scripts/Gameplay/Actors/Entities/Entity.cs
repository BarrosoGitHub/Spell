using System;
using System.Collections.Generic;
using UnityEngine;

public class Entity
{
    private Stats baseStats;
    private readonly ControllerStats controllerStats;
    public Stats stats { get; private set; }
    private readonly List<Equipment> equipmentList;
    private readonly Dictionary<object, Stats> statModifiers = new Dictionary<object, Stats>();

    public Action<float> OnHealthChanged;
    public Action OnEntityDied;

    private float currentExperience;
    public float LevelUpExperience => stats.Level * 2f;

    private float currentHealth;
    public float CurrentHealth => currentHealth;

    public Entity(List<Equipment> equipmentList = null, ControllerStats controllerStats = null)
    {
        if (controllerStats == null)
            throw new ArgumentNullException(nameof(controllerStats), "Assign a ControllerStats asset before creating an Entity.");

        baseStats = controllerStats.CreateBaseStats();
        this.controllerStats = controllerStats;
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

    public void SetLevel(int level)
    {
        baseStats.Level = Mathf.Max(1, level);
        currentExperience = 0f;
        UpdateStats();
    }

    public void ResetLevel() => SetLevel(1);

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
        // Recalculate from the base on every change, including pooled level resets.
        stats = baseStats.Add(controllerStats.GetBonus(baseStats.Level));

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
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, Mathf.Max(0f, stats.MaxHealth));

        if (currentHealth <= 0f && previousHealth > 0f)
            OnEntityDied?.Invoke();

        if (previousHealth != currentHealth)
            OnHealthChanged?.Invoke(currentHealth);

        return currentHealth;
    }
}
