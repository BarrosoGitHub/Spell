using UnityEngine;

[DisallowMultipleComponent]
public sealed class OptimizationManager : MonoBehaviour
{
    [SerializeField] private bool animationLod = true;
    [SerializeField] private ControllerSpawner spawner;
    [SerializeField] private Transform distanceOrigin;

    private GameObject lodObject;

    private void Update()
    {
        if (animationLod == (lodObject != null)) return;

        if (!animationLod)
        {
            RemoveLod();
            return;
        }

        if (spawner == null || distanceOrigin == null)
        {
            Debug.LogError("Assign a spawner and distance origin to OptimizationManager.", this);
            animationLod = false;
            return;
        }

        lodObject = new GameObject("Animation LOD");
        lodObject.transform.SetParent(transform, false);
        var distances = lodObject.AddComponent<DistanceManager>();
        var manager = lodObject.AddComponent<AnimationLodManager>();
        manager.Initialize(spawner, distanceOrigin, distances);
    }

    private void OnDisable() => RemoveLod();

    private void RemoveLod()
    {
        if (lodObject == null) return;
        lodObject.SetActive(false);
        Destroy(lodObject);
        lodObject = null;
    }
}
