using System;
using System.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Navigation/City NavMesh Baker")]
public sealed class CityNavMeshBaker : MonoBehaviour
{
    [Tooltip("Bake active child buildings when play starts. Leave off for saved editor bakes.")]
    public bool bakeOnStart;
    public bool IsBaking { get; private set; }
    public string Status { get; private set; }
    private bool stopRequested;

    private void Start() { if (bakeOnStart) BakeAll(); }
    private void OnDisable() => StopAfterCurrentBuilding();

    [ContextMenu("Bake All Buildings")]
    public async void BakeAll() => await BakeAllAsync();

    [ContextMenu("Force Rebuild All Buildings")]
    public async void ForceRebuildAll() => await BakeAllAsync(true);

    public void StopAfterCurrentBuilding() => stopRequested = true;

    public async Task BakeAllAsync(bool force = false)
    {
        if (IsBaking) return;
        IsBaking = true;
        stopRequested = false;
        int completed = 0, failed = 0, skipped = 0;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var buildings = GetComponentsInChildren<BuildingNavMeshBaker>(true);
            for (int i = 0; i < buildings.Length && !stopRequested && this != null; i++)
            {
                var building = buildings[i];
                if (building == null || !building.gameObject.activeInHierarchy)
                {
                    skipped++;
                    continue;
                }
                Status = $"{i + 1}/{buildings.Length}: {building.name}";
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.RecordObject(building, "Enable Building NavMesh Baker");
#endif
                building.enabled = true;
                if (await building.BakeAsync(force, false)) completed++;
                else failed++;
#if UNITY_EDITOR
                if (building != null && !Application.isPlaying)
                    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(building);
#endif
            }
        }
        catch (Exception exception) { Debug.LogException(exception); failed++; }
        finally
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.AssetDatabase.SaveAssets();
#endif
            IsBaking = false;
            Status = $"{completed} baked, {skipped} inactive, {failed} failed, {timer.Elapsed.TotalSeconds:F1}s";
            Debug.Log($"City bake {(stopRequested ? "stopped" : "finished")}: {Status}.");
        }
    }
}
