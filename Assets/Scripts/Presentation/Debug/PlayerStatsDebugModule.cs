using UnityEngine;

public sealed class PlayerStatsDebugModule
{
    private const float RefreshInterval = 0.25f;
    private float nextRefresh;
    private string statsText = "Waiting for player...";
    private GUIStyle panelStyle;

    public void Refresh(PlayerController player)
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + RefreshInterval;
        if (player == null || player.entity == null)
        {
            statsText = "Waiting for player...";
            return;
        }

        Entity entity = player.entity;
        Stats stats = entity.stats;
        float damage = stats.Damage;
        float attackSpeed = Mathf.Max(0.01f, stats.EffectiveAttackSpeed);
        float defense = stats.EffectiveDefense;
        statsText = $"PLAYER STATS — DEBUG\n\n" +
            $"Level   {entity.Level}\n" +
            $"Health   {entity.CurrentHealth:0.#} / {stats.MaxHealth:0.#}\n\n" +
            $"Ice damage   {damage:0.##}\n" +
            $"Range   {stats.Range:0.##}\n" +
            $"Casts / second   {attackSpeed:0.##}\n" +
            $"Cast cooldown   {1f / attackSpeed:0.###} s\n" +
            $"Critical chance   {stats.CriticalChance:0.##}%\n" +
            $"Multishot chance   {stats.MultishotChance:0.##}%\n" +
            $"Critical damage   {stats.CriticalDamage:0.##}x\n" +
            $"Defense   {defense:0.##}\n\n" +
            $"TOTAL FLAT / PERCENT BONUSES\n" +
            $"Damage   {stats.BaseDamage:0.##} / {stats.BaseDamagePerc:P0}\n" +
            $"Attack speed   {stats.AttackSpeed:0.##} / {stats.AttackSpeedPerc:P0}\n" +
            $"Defense   {stats.Defense:0.##} / {stats.DefensePerc:P0}\n" +
            $"Health bonus   {stats.MaxHealthPerc:P0}";
    }

    public void Draw()
    {
        if (panelStyle == null)
        {
            panelStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 14,
                padding = new RectOffset(14, 14, 12, 12),
                richText = false
            };
            panelStyle.normal.textColor = Color.white;
        }
        GUI.Box(new Rect(Mathf.Max(8f, Screen.width - 308f), 16f, 292f, 380f), statsText, panelStyle);
    }
}
