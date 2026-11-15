using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DistanceManager : MonoBehaviour
{
    private readonly Dictionary<Transform, float> squaredDistances = new(1000);

    public void Refresh(ControllerSpawner spawner, Transform origin)
    {
        squaredDistances.Clear();
        if (spawner == null || origin == null) return;

        Vector3 position = origin.position;
        var controllers = spawner.SpawnedControllers;
        for (int i = 0; i < controllers.Count; i++)
        {
            if (controllers[i] is EnemyController enemy && enemy.isActiveAndEnabled)
            {
                Transform target = enemy.transform;
                squaredDistances[target] = (target.position - position).sqrMagnitude;
            }
        }
    }

    public bool TryGetSquaredDistance(Transform target, out float distance)
    {
        distance = 0f;
        return target != null && squaredDistances.TryGetValue(target, out distance);
    }

    private void OnDisable() => squaredDistances.Clear();
}
