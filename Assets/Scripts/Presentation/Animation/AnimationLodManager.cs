using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class AnimationLodManager : MonoBehaviour
{
    [SerializeField, Min(0f)] private float nearDistance = 15f;
    [SerializeField, Min(0f)] private float farDistance = 40f;
    [SerializeField, Min(1f)] private float nearUpdatesPerSecond = 30f;
    [SerializeField, Min(1f)] private float middleUpdatesPerSecond = 15f;
    [SerializeField, Min(1f)] private float farUpdatesPerSecond = 5f;

    private Transform origin;
    private DistanceManager distances;

    private struct Entry
    {
        public Controller controller;
        public Transform target;
        public AnimatorFrequency animation;
        public float elapsed;
        public float remaining;
    }

    private readonly List<Entry> entries = new(1000);
    private readonly Dictionary<Controller, int> indices = new(1000);
    private ControllerSpawner spawner;
    private float nearDistanceSquared;
    private float farDistanceSquared;
    private float nearInterval;
    private float middleInterval;
    private float farInterval;

    public void Initialize(ControllerSpawner source, Transform distanceOrigin, DistanceManager distanceManager)
    {
        spawner = source;
        origin = distanceOrigin;
        distances = distanceManager;
        spawner.OnControllerSpawned += Register;
        spawner.OnControllerDespawned += Unregister;
        foreach (Controller controller in spawner.SpawnedControllers)
            Register(controller);
    }

    private void Register(Controller controller)
    {
        if (controller is not EnemyController || indices.ContainsKey(controller)) return;
        var animation = controller.transform.root.GetComponentInChildren<AnimatorFrequency>(true);
        if (animation == null) return;

        animation.SetManualEvaluation(true);
        indices.Add(controller, entries.Count);
        entries.Add(new Entry
        {
            controller = controller,
            target = controller.transform,
            animation = animation,
            // Spread deadlines without advancing the actual animation clock.
            remaining = Mathf.Repeat(entries.Count * 0.6180339f, 1f) / Mathf.Max(1f, nearUpdatesPerSecond)
        });
    }

    private void Unregister(Controller controller)
    {
        if (!indices.Remove(controller, out int index)) return;
        if (entries[index].animation != null)
            entries[index].animation.SetManualEvaluation(false);

        int last = entries.Count - 1;
        if (index != last)
        {
            entries[index] = entries[last];
            indices[entries[index].controller] = index;
        }
        entries.RemoveAt(last);
    }

    private void Update()
    {
        if (spawner == null || distances == null) return;
        distances.Refresh(spawner, origin);
        nearDistanceSquared = nearDistance * nearDistance;
        float far = Mathf.Max(nearDistance, farDistance);
        farDistanceSquared = far * far;
        nearInterval = 1f / Mathf.Max(1f, nearUpdatesPerSecond);
        middleInterval = 1f / Mathf.Max(1f, middleUpdatesPerSecond);
        farInterval = 1f / Mathf.Max(1f, farUpdatesPerSecond);
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry.animation == null || entry.controller == null)
            {
                Unregister(entry.controller);
                i--;
                continue;
            }
            if (!entry.animation.isActiveAndEnabled || !entry.controller.isActiveAndEnabled)
                continue;

            entry.animation.SetManualEvaluation(true);
            float interval = nearInterval;
            if (distances.TryGetSquaredDistance(entry.target, out float distance))
                interval = distance <= nearDistanceSquared ? nearInterval :
                    distance <= farDistanceSquared ? middleInterval : farInterval;

            entry.elapsed += deltaTime;
            entry.remaining = Mathf.Min(entry.remaining, interval) - deltaTime;
            if (entry.remaining <= 0f && entry.elapsed > 0f)
            {
                float elapsed = entry.elapsed;
                entry.elapsed = 0f;
                entry.remaining += interval;
                if (entry.remaining <= 0f) entry.remaining = interval;
                entries[i] = entry;
                entry.animation.Tick(elapsed);
                // Animation events can despawn an enemy and swap another entry into this slot.
                if (i >= entries.Count || entries[i].controller != entry.controller)
                    i--;
                continue;
            }
            entries[i] = entry;
        }
    }

    private void OnDisable()
    {
        if (spawner != null)
        {
            spawner.OnControllerSpawned -= Register;
            spawner.OnControllerDespawned -= Unregister;
        }
        foreach (Entry entry in entries)
            if (entry.animation != null) entry.animation.SetManualEvaluation(false);
        entries.Clear();
        indices.Clear();
        spawner = null;
    }
}
