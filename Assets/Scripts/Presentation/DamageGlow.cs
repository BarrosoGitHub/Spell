using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageGlow : MonoBehaviour
{
    [SerializeField] private Controller controller;
    [Tooltip("Root containing the character meshes. Defaults to this object.")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("Optional explicit renderers. Otherwise character meshes are discovered once at setup.")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField, Min(0.01f)] private float durationSeconds = 0.18f;
    [SerializeField, ColorUsage(false, true)] private Color glowColor = new Color(3f, 0f, 0f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private sealed class RendererMaterials
    {
        public Renderer Renderer;
        public Material[] Original;
        public Material[] Glow;
    }

    private RendererMaterials[] cachedMaterials;
    private InteractionManager interactionManager;
    private bool glowing;
    private float glowEndTime;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<Controller>();
        if (controller == null)
        {
            Debug.LogError("DamageGlow requires a Controller reference to identify received damage.", this);
            enabled = false;
            return;
        }

        if (visualRoot == null)
            visualRoot = transform;
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (targetRenderers.Length == 0)
                targetRenderers = visualRoot.GetComponentsInChildren<MeshRenderer>(true);
        }

        // Own these copies for the component lifetime; hits only swap cached arrays.
        cachedMaterials = new RendererMaterials[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer target = targetRenderers[i];
            if (target == null) continue;

            Material[] original = target.sharedMaterials;
            Material[] glow = new Material[original.Length];
            for (int j = 0; j < original.Length; j++)
            {
                if (original[j] == null) continue;

                Material material = new Material(original[j]);
                glow[j] = material;
                if (material.HasProperty(BaseColorId))
                    material.SetColor(BaseColorId, Color.red);
                else if (material.HasProperty(ColorId))
                    material.SetColor(ColorId, Color.red);
                if (material.HasProperty(EmissionColorId))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor(EmissionColorId, glowColor);
                }
            }

            cachedMaterials[i] = new RendererMaterials { Renderer = target, Original = original, Glow = glow };
        }
    }

    private void OnEnable()
    {
        if (controller == null) return;
        interactionManager = InteractionManager.Instance;
        interactionManager.OnInteractionReceived += HandleInteractionReceived;
    }

    private void HandleInteractionReceived(Controller target, Controller source, InteractionContext context)
    {
        if (target != controller || context == null || context.Type != InteractionType.Attack || context.Value <= 0f)
            return;

        glowEndTime = Time.time + Mathf.Max(0.01f, durationSeconds);
        if (glowing) return;

        glowing = true;
        foreach (RendererMaterials materials in cachedMaterials)
        {
            if (materials != null && materials.Renderer != null)
                materials.Renderer.sharedMaterials = materials.Glow;
        }
    }

    private void Update()
    {
        if (glowing && Time.time >= glowEndTime)
            RestoreMaterials();
    }

    private void RestoreMaterials()
    {
        if (!glowing) return;
        glowing = false;
        glowEndTime = 0f;
        foreach (RendererMaterials materials in cachedMaterials)
        {
            if (materials != null && materials.Renderer != null)
                materials.Renderer.sharedMaterials = materials.Original;
        }
    }

    private void OnDisable()
    {
        if (interactionManager != null)
            interactionManager.OnInteractionReceived -= HandleInteractionReceived;
        interactionManager = null;
        RestoreMaterials();
    }

    private void OnDestroy()
    {
        if (cachedMaterials == null) return;
        foreach (RendererMaterials materials in cachedMaterials)
        {
            if (materials == null) continue;
            foreach (Material material in materials.Glow)
                if (material != null) Destroy(material);
        }
    }
}
