using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyLabelsDebugModule
{
    private sealed class Entry
    {
        public EnemyController Enemy;
        public Transform Head;
        public int Level;
        public string Label;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private GUIStyle style;

    public void Refresh()
    {
        // Keep discovery outside rendering and retain no references to despawned objects.
        entries.Clear();
        foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>())
        {
            Animator animator = enemy.GetComponentInChildren<Animator>();
            entries.Add(new Entry
            {
                Enemy = enemy,
                Head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null
            });
        }
    }

    public void Draw(Camera camera, bool pursuit, bool levels)
    {
        if (camera == null || Event.current.type != EventType.Repaint) return;
        if (style == null)
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold };

        foreach (Entry entry in entries)
        {
            EnemyController enemy = entry.Enemy;
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.entity == null || enemy.entity.CurrentHealth <= 0f) continue;
            Vector3 position = entry.Head != null ? entry.Head.position + Vector3.up * 0.45f : enemy.transform.position + Vector3.up * 2.3f;
            Vector3 screen = camera.WorldToScreenPoint(position);
            if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height) continue;
            Rect rect = new Rect(screen.x - 70f, Screen.height - screen.y - 14f, 140f, 24f);
            if (levels)
            {
                if (entry.Label == null || entry.Level != enemy.entity.Level)
                {
                    entry.Level = enemy.entity.Level;
                    entry.Label = "Lv. " + entry.Level;
                }
                Label(rect, entry.Label, Color.white);
                rect.y += 20f;
            }
            if (pursuit && enemy.State == enemy.pursuingState && enemy.Target is PlayerController)
                Label(rect, "PURSUING", new Color(1f, 0.65f, 0.1f));
        }
    }

    private void Label(Rect rect, string text, Color color)
    {
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
    }

    public void Clear() => entries.Clear();
}
