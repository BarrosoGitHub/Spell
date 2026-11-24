using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

public sealed class DamageNumbersDebugModule
{
    private const int Capacity = 64;
    private readonly AnimationCurve rise = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    private readonly AnimationCurve fade = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.65f, 1f), new Keyframe(1f, 0f));
    private readonly AnimationCurve scale = new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.12f, 1.15f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1f));
    private EntityManager interactions;
    private Camera viewCamera;
    private GameObject root;
    private MMFloatingTextMeshPro[] numbers;
    private int next;

    public void Bind(EntityManager source, Camera camera, Transform owner)
    {
        viewCamera = camera;
        if (interactions == source) return;
        Clear();
        interactions = source;
        if (!interactions) return;
        root = new GameObject("Debug Damage Numbers");
        root.transform.SetParent(owner, false);
        numbers = new MMFloatingTextMeshPro[Capacity];
        for (int i = 0; i < Capacity; i++)
        {
            var item = new GameObject("Damage Number");
            item.SetActive(false);
            item.transform.SetParent(root.transform, false);
            var moving = new GameObject("Moving").transform;
            moving.SetParent(item.transform, false);
            var label = new GameObject("Text").AddComponent<TextMeshPro>();
            label.transform.SetParent(moving, false);
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 3.5f;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.rectTransform.sizeDelta = new Vector2(4f, 1f);
            var number = item.AddComponent<MMFloatingTextMeshPro>();
            number.MovingPart = moving;
            number.Billboard = label.transform;
            number.TargetTextMeshPro = label;
            numbers[i] = number;
        }
        interactions.OnInteraction += OnInteraction;
    }

    private void OnInteraction(Controller source, Controller target, InteractionContext context)
    {
        if (!target || !viewCamera || context.Type != InteractionType.Attack || context.Value <= 0f) return;
        MMFloatingTextMeshPro number = numbers[next];
        next = (next + 1) % Capacity;
        number.gameObject.SetActive(false);
        number.transform.position = target.transform.position + Vector3.up * 1.5f;
        number.MovingPart.localPosition = Vector3.zero;
        number.MovingPart.localScale = Vector3.one;
        number.TargetTextMeshPro.color = context.IsCritical ? Color.red : Color.white;
        number.gameObject.SetActive(true);
        number.SetProperties(context.Value.ToString("0.#"), 0.7f, Vector3.up, true,
            MMFloatingTextSpawner.AlignmentModes.Fixed, Vector3.up, true, viewCamera,
            true, rise, 0f, ((next % 5) - 2) * 0.18f,
            true, rise, 0f, 1.2f, false, rise, 0f, 0f,
            true, fade, 0f, 1f, true, scale, 0f, context.IsCritical ? 1.25f : 1f, false, null);
    }

    public void Clear()
    {
        if (interactions) interactions.OnInteraction -= OnInteraction;
        interactions = null;
        if (root)
        {
            root.SetActive(false);
            Object.Destroy(root);
        }
        root = null;
        numbers = null;
        next = 0;
    }
}
