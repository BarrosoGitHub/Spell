public class Stats
{
    public int Level { get; set; }
    public float MaxHealth { get; private set; }
    public float BaseDamage { get; private set; }
    public float AttackSpeed { get; private set; }
    public float Defense { get; private set; }
    public float CriticalChance { get; private set; }
    public float CriticalDamage { get; private set; }

    public float MaxHealthPerc { get; private set; }
    public float BaseDamagePerc { get; private set; }
    public float AttackSpeedPerc { get; private set; }
    public float DefensePerc { get; private set; }

    public Stats(int level = 1,
                 float maxHealth = 0, 
                 float baseDamage = 0, 
                 float attackSpeed = 0, 
                 float defense = 0, 
                 float criticalChance = 0, 
                 float criticalDamage = 0,
                 float maxHealthPerc = 0,
                 float baseDamagePerc = 0,
                 float attackSpeedPerc = 0,
                 float defensePerc = 0)
    {
        Level = level;
        MaxHealth = maxHealth;
        BaseDamage = baseDamage;
        AttackSpeed = attackSpeed;
        Defense = defense;
        CriticalChance = criticalChance;
        CriticalDamage = criticalDamage;
        MaxHealthPerc = maxHealthPerc;
        BaseDamagePerc = baseDamagePerc;
        AttackSpeedPerc = attackSpeedPerc;
        DefensePerc = defensePerc;
    }

    public Stats Add(Stats other)
    {
        return new Stats(
            Level,
            MaxHealth + other.MaxHealth,
            BaseDamage + other.BaseDamage,
            AttackSpeed + other.AttackSpeed,
            Defense + other.Defense,
            CriticalChance + other.CriticalChance,
            CriticalDamage + other.CriticalDamage,
            MaxHealthPerc + other.MaxHealthPerc,
            BaseDamagePerc + other.BaseDamagePerc,
            AttackSpeedPerc + other.AttackSpeedPerc,
            DefensePerc + other.DefensePerc
        );
    }
}