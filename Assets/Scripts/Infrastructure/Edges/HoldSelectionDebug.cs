using UnityEngine;

[DisallowMultipleComponent]
public sealed class HoldSelectionDebug : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int ColorProperty = Shader.PropertyToID("_Color");

    bool selected;
    bool grabbable;
    Renderer[] renderers;
    MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        propertyBlock = new MaterialPropertyBlock();
    }

    public void SetSelected(bool value)
    {
        selected = value;
        UpdateColor();
    }

    public void SetGrabbable(bool value)
    {
        grabbable = value;
        UpdateColor();
    }

    void UpdateColor()
    {
        if (renderers == null) Awake();
        foreach (Renderer renderer in renderers)
        {
            if (!renderer) continue;
            if (!selected && !grabbable)
            {
                renderer.SetPropertyBlock(null);
                continue;
            }

            renderer.GetPropertyBlock(propertyBlock);
            Material material = renderer.sharedMaterial;
            if (material && material.HasProperty(BaseColor)) propertyBlock.SetColor(BaseColor, grabbable ? Color.green : new Color(.75f, .1f, 1f, 1f));
            if (material && material.HasProperty(ColorProperty)) propertyBlock.SetColor(ColorProperty, grabbable ? Color.green : new Color(.75f, .1f, 1f, 1f));
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    void OnDisable()
    {
        selected = false;
        grabbable = false;
        UpdateColor();
    }
}
