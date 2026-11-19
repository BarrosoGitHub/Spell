using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup), typeof(Button))]
public sealed class RuneRewardCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private CanvasGroup group;
    private RectTransform rect;
    private float elapsed;
    private float delay;
    private bool hovered;
    private bool selected;
    private float scale = 0.92f;

    public void Reveal(float entranceDelay)
    {
        group = GetComponent<CanvasGroup>();
        rect = (RectTransform)transform;
        delay = entranceDelay;
        elapsed = 0f;
        hovered = selected = false;
        scale = 0.92f;
        group.alpha = 0f;
        group.interactable = false;
        rect.localScale = Vector3.one * scale;
    }

    public void Choose()
    {
        selected = true;
        group.interactable = false;
    }

    private void Update()
    {
        if (group == null) return;
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01((elapsed - delay) / 0.28f);
        group.alpha = Mathf.SmoothStep(0f, 1f, progress);
        group.interactable = progress >= 1f && !selected;
        float target = selected ? 1.07f : hovered ? 1.025f : 1f;
        scale = Mathf.Lerp(scale, target, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        rect.localScale = Vector3.one * scale;
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;
    public void OnSelect(BaseEventData eventData) => hovered = true;
    public void OnDeselect(BaseEventData eventData) => hovered = false;
}
