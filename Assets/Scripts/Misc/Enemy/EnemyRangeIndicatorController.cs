using UnityEngine;

[DisallowMultipleComponent]
public class EnemyRangeIndicatorController : MonoBehaviour
{
    private enum IndicatorAttackType
    {
        Cone,
        Directional,
        Circle
    }

    [Header("Indicator")]
    [SerializeField] private Renderer indicatorRenderer;

    [Header("Attack Materials")]
    [SerializeField] private Material coneMaterial;
    [SerializeField] private Material directionalMaterial;
    [SerializeField] private Material circleMaterial;

    [Header("Shader Settings")]
    [SerializeField] private Color indicatorColor = new Color(1f, 0.2f, 0.2f, 0.45f);
    [SerializeField] private Color fillColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField, Min(0f)] private float range = 5f;
    [SerializeField, Range(1f, 89f)] private float halfAngleDeg = 30f;
    [SerializeField, Min(0f)] private float edgeSoftness = 0.35f;
    [SerializeField, Range(0f, 1f)] private float fill;

    [SerializeField] private EnemyController enemyController;

    private MaterialPropertyBlock materialPropertyBlock;
    private Interpolate fillTween;
    private IndicatorAttackType? currentIndicatorAttackType;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
    private static readonly int RangeId = Shader.PropertyToID("_Range");
    private static readonly int HalfAngleDegId = Shader.PropertyToID("_HalfAngleDeg");
    private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int FillId = Shader.PropertyToID("_Fill");

    private void Awake()
    {
        if (indicatorRenderer == null)
        {
            indicatorRenderer = GetComponentInChildren<Renderer>(true);
        }

        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }

        materialPropertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        ApplyToMaterial();
        Hide();

        if (enemyController != null)
        {
            enemyController.OnPreparingActionInstance += OnAttackPreparing;
            enemyController.OnExecutingActionInstance += OnAttackExecuting;
            enemyController.OnCompleteActionInstance += OnAttackComplete;
            enemyController.OnCancelActionInstance += OnAttackComplete;
        }
    }

    public void SetRange(float newRange)
    {
        range = Mathf.Max(0f, newRange);
        ApplyToMaterial();
    }

    public void SetHalfAngle(float newHalfAngleDeg)
    {
        halfAngleDeg = Mathf.Clamp(newHalfAngleDeg, 1f, 89f);
        ApplyToMaterial();
    }

    public void SetEdgeSoftness(float newEdgeSoftness)
    {
        edgeSoftness = Mathf.Max(0f, newEdgeSoftness);
        ApplyToMaterial();
    }

    public void SetColor(Color newColor)
    {
        indicatorColor = newColor;
        ApplyToMaterial();
    }

    public void SetFillColor(Color newFillColor)
    {
        fillColor = newFillColor;
        ApplyToMaterial();
    }

    public void SetFill(float newFill)
    {
        fill = Mathf.Clamp01(newFill);
        ApplyToMaterial();
    }

    public void ResetFill()
    {
        fill = 0f;
        ApplyToMaterial();
    }

    public void Configure(float newRange, float newHalfAngleDeg, float newEdgeSoftness, Color newColor)
    {
        range = Mathf.Max(0f, newRange);
        halfAngleDeg = Mathf.Clamp(newHalfAngleDeg, 1f, 89f);
        edgeSoftness = Mathf.Max(0f, newEdgeSoftness);
        indicatorColor = newColor;
        ApplyToMaterial();
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (indicatorRenderer == null)
        {
            return;
        }

        if (indicatorRenderer.gameObject.activeSelf != visible)
        {
            indicatorRenderer.gameObject.SetActive(visible);
        }
    }

    private void OnAttackPreparing(float preparingTime, ActionInstance actionInstance)
    {
        if (!TryResolveAttackType(actionInstance, out IndicatorAttackType attackType))
        {
            return;
        }

        SwitchMaterialForAttackType(attackType);
        currentIndicatorAttackType = attackType;

        if (actionInstance is AttackDirectional directionalAttack)
        {
            range = directionalAttack.Range;
            halfAngleDeg = directionalAttack.HalfAngleDeg;
        }

        Show();

        if (fillTween != null) fillTween.Cancel();
        fill = 0f;
        ApplyToMaterial();
        fillTween = gameObject.ToFloat(() => fill, x => { fill = x; ApplyToMaterial(); }, 1f, preparingTime)
            .SetEase(Ease.Linear);
    }

    private void OnAttackExecuting(float executingTime, ActionInstance actionInstance)
    {
        if (!IsCurrentAttack(actionInstance))
        {
            return;
        }

        if (fillTween != null) fillTween.Cancel();
        SetFill(1f);
    }

    private void OnAttackComplete(ActionInstance actionInstance)
    {
        if (!IsCurrentAttack(actionInstance))
        {
            return;
        }

        if (fillTween != null) fillTween.Cancel();
        Hide();
        ResetFill();
        currentIndicatorAttackType = null;
    }

    private bool IsCurrentAttack(ActionInstance actionInstance)
    {
        if (!TryResolveAttackType(actionInstance, out IndicatorAttackType attackType))
        {
            return false;
        }

        return currentIndicatorAttackType.HasValue && currentIndicatorAttackType.Value == attackType;
    }

    private bool TryResolveAttackType(ActionInstance actionInstance, out IndicatorAttackType attackType)
    {
        if (actionInstance is AttackWithCone)
        {
            attackType = IndicatorAttackType.Cone;
            return true;
        }

        if (actionInstance is AttackDirectional)
        {
            attackType = IndicatorAttackType.Directional;
            return true;
        }

        if (actionInstance is AttackCircle)
        {
            attackType = IndicatorAttackType.Circle;
            return true;
        }

        attackType = default;
        return false;
    }

    private void SwitchMaterialForAttackType(IndicatorAttackType attackType)
    {
        if (indicatorRenderer == null)
        {
            return;
        }

        Material targetMaterial = attackType switch
        {
            IndicatorAttackType.Cone => coneMaterial,
            IndicatorAttackType.Directional => directionalMaterial,
            IndicatorAttackType.Circle => circleMaterial,
            _ => null
        };

        if (targetMaterial == null || indicatorRenderer.sharedMaterial == targetMaterial)
        {
            return;
        }

        indicatorRenderer.sharedMaterial = targetMaterial;
    }

    private void ApplyToMaterial()
    {
        if (indicatorRenderer == null)
        {
            return;
        }

        indicatorRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor(ColorId, indicatorColor);
        materialPropertyBlock.SetColor(FillColorId, fillColor);
        materialPropertyBlock.SetFloat(RangeId, range);
        materialPropertyBlock.SetFloat(HalfAngleDegId, halfAngleDeg);
        materialPropertyBlock.SetFloat(EdgeSoftnessId, edgeSoftness);
        materialPropertyBlock.SetFloat(FillId, fill);
        indicatorRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void OnDisable()
    {
        if (fillTween != null) fillTween.Cancel();
        fillTween = null;
        currentIndicatorAttackType = null;
        Hide();
        if (materialPropertyBlock != null) ResetFill();
    }

    private void OnDestroy()
    {
        if (fillTween != null) fillTween.Cancel();
        if (enemyController != null)
        {
            enemyController.OnPreparingActionInstance -= OnAttackPreparing;
            enemyController.OnExecutingActionInstance -= OnAttackExecuting;
            enemyController.OnCompleteActionInstance -= OnAttackComplete;
            enemyController.OnCancelActionInstance -= OnAttackComplete;
        }
    }
}
