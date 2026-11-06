using UnityEngine;

public class NearbyHoldChunkLoader : MonoBehaviour
{
    [Min(0f)] public float loadRadius = 20f;
    [Min(.02f)] public float refreshInterval = .2f;
    public EdgeSpawner edgeSpawner;

    float nextRefresh;

    void Awake()
    {
        if (!edgeSpawner)
            edgeSpawner = FindAnyObjectByType<EdgeSpawner>();
    }

    void OnEnable()
    {
        nextRefresh = 0f;
    }

    void Update()
    {
        if (!edgeSpawner || Time.time < nextRefresh) return;
        nextRefresh = Time.time + Mathf.Max(.02f, refreshInterval);
        edgeSpawner.SetChunksNearPosition(transform.position, loadRadius);
    }

    void OnDisable()
    {
        if (edgeSpawner)
            edgeSpawner.DespawnAllChunks();
    }
}
