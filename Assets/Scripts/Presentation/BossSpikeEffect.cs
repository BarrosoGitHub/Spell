using UnityEngine;

public sealed class BossSpikeEffect : MonoBehaviour
{
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private Material warningMaterial;
    private GameObject spike;
    private GameObject warning;
    private float elapsed;
    private bool erupting;

    private void Awake()
    {
        spike = Instantiate(spikePrefab);
        warning = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        warning.name = "Spike Warning";
        Destroy(warning.GetComponent<Collider>());
        warning.GetComponent<Renderer>().sharedMaterial = warningMaterial;
        Clear();
    }

    public void ShowWarning(Vector3 position, float radius)
    {
        if (spike == null) return;
        Clear();
        spike.transform.position = position;
        warning.transform.position = position + Vector3.up * 0.035f;
        warning.transform.localScale = new Vector3(radius * 2f, 0.015f, radius * 2f);
        warning.SetActive(true);
    }

    public void Erupt()
    {
        if (spike == null) return;
        warning.SetActive(false);
        spike.transform.localScale = new Vector3(1f, 0.01f, 1f);
        spike.SetActive(true);
        elapsed = 0f;
        erupting = true;
    }

    private void Update()
    {
        if (!erupting) return;
        elapsed += Time.deltaTime;
        float height = elapsed < 0.15f ? Mathf.Clamp01(elapsed / 0.15f)
            : 1f - Mathf.Clamp01((elapsed - 0.5f) / 0.35f);
        spike.transform.localScale = new Vector3(1f, Mathf.Max(0.01f, height), 1f);
    }

    public void Clear()
    {
        erupting = false;
        if (spike != null) spike.SetActive(false);
        if (warning != null) warning.SetActive(false);
    }

    private void OnDisable() => Clear();
    private void OnDestroy()
    {
        if (spike != null) Destroy(spike);
        if (warning != null) Destroy(warning);
    }
}
