using UnityEngine;
using UnityEngine.Rendering;

public sealed class AttackRangeDebugModule
{
    private const int Segments = 96;
    private LineRenderer line;
    private Material material;
    private float lastRange = -1f;

    public void Refresh(PlayerController player, bool visible)
    {
        if (!visible || player == null || !player.isActiveAndEnabled || player.entity == null ||
            player.entity.CurrentHealth <= 0f)
        {
            if (line != null) line.enabled = false;
            return;
        }
        if (line == null)
        {
            var root = new GameObject("Debug Attack Range");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, player.gameObject.scene);
            line = root.AddComponent<LineRenderer>();
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(0.2f, 0.85f, 1f, 1f));
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Segments;
            line.widthMultiplier = 0.045f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
        }
        float range = Mathf.Max(0f, player.entity.stats.Range);
        line.enabled = range > 0f;
        Vector3 center = player.transform.position;
        center.y = player.groundHit.collider != null ? player.groundHit.point.y : center.y - 0.5f;
        line.transform.position = center + Vector3.up * 0.06f;
        if (Mathf.Approximately(range, lastRange)) return;
        lastRange = range;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * range, 0f, Mathf.Sin(angle) * range));
        }
    }

    public void Clear()
    {
        if (line != null) Object.Destroy(line.gameObject);
        if (material != null) Object.Destroy(material);
        line = null;
        material = null;
        lastRange = -1f;
    }
}
