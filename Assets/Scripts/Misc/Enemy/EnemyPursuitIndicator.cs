using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyPursuitIndicator : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private Color color = new Color(1f, 0.65f, 0.1f);
    [SerializeField] private float height = 2.3f;

    private EnemyController enemy;
    private Camera viewCamera;
    private GUIStyle labelStyle;

    private void Awake()
    {
        enemy = GetComponent<EnemyController>();
        Animator animator = GetComponentInChildren<Animator>();
        if (head == null && animator != null && animator.isHuman)
            head = animator.GetBoneTransform(HumanBodyBones.Head);
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || enemy == null ||
            enemy.State != enemy.pursuingState || !(enemy.Target is PlayerController)) return;

        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null) return;

        Vector3 position = head != null
            ? head.position + Vector3.up * 0.4f
            : transform.position + Vector3.up * height;
        Vector3 screen = viewCamera.WorldToScreenPoint(position);
        if (screen.z <= 0f) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }

        Rect label = new Rect(screen.x - 60f, Screen.height - screen.y - 12f, 120f, 24f);
        labelStyle.normal.textColor = Color.black;
        GUI.Label(new Rect(label.x + 1f, label.y + 1f, label.width, label.height), "PURSUING", labelStyle);
        labelStyle.normal.textColor = color;
        GUI.Label(label, "PURSUING", labelStyle);
    }
}
