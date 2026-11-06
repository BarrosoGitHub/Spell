using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>Bakes walkable building tops and links their edges to lower walkable surfaces.</summary>
[ExecuteAlways, DisallowMultipleComponent]
[AddComponentMenu("Navigation/Building NavMesh Baker")]
public sealed class BuildingNavMeshBaker : MonoBehaviour
{
    [Tooltip("Must match the enemy's NavMeshAgent type.")]
    public int agentTypeID;
    [Tooltip("Only geometry below this building, on these layers, is collected.")]
    public LayerMask geometryLayers = ~0;
    [Tooltip("Nearby objects on these layers block the bake, but never become walkable unless they also belong to this building's Geometry Layers.")]
    public LayerMask blockingLayers = ~0;
    public NavMeshCollectGeometry geometry = NavMeshCollectGeometry.RenderMeshes;
    [Tooltip("Uses two voxels per agent radius instead of three. Faster, with less detail around narrow edges.")]
    public bool fastBake = true;
    [Tooltip("Optional runtime bake. Normally use the Inspector Bake button and save the scene.")]
    public bool bakeOnStart;

    [Header("Automatic Links")]
    [Min(1f)] public float linkSpacing = 4f;
    [Min(1f)] public float maximumLinkDrop = 50f;

    [SerializeField, HideInInspector] private NavMeshData[] bakedData = Array.Empty<NavMeshData>();
    [SerializeField, HideInInspector] private Transform generatedLinksRoot;
    [SerializeField, HideInInspector] private int bakedAgentTypeID = int.MinValue;
    private NavMeshData[] ownedData;
    private bool destroyed;
    private readonly List<NavMeshDataInstance> instances = new List<NavMeshDataInstance>();

    private static readonly Vector3[] UpDirections =
        { Vector3.up };
    private static readonly string[] FaceNames = { "Roof" };

    public int BakedSurfaceCount => bakedData.Length > 0 && bakedData[0] != null ? 1 : 0;
    public int GeneratedLinkCount => generatedLinksRoot != null ? generatedLinksRoot.childCount : 0;
    public bool IsBaking { get; private set; }

    private void OnEnable() { if (!IsBaking) AddData(); }
    private void OnDisable() => RemoveData();
    private void Start()
    {
        var city = GetComponentInParent<CityNavMeshBaker>();
        if (Application.isPlaying && bakeOnStart &&
            (city == null || !city.isActiveAndEnabled || !city.bakeOnStart))
            Bake();
    }

    [ContextMenu("Bake Roof")]
    public async void Bake() => await BakeAsync();

    [ContextMenu("Force Rebuild Roof")]
    public async void ForceRebuild() => await BakeAsync(true);

    public async Task<bool> BakeAsync(bool force = false, bool saveAssets = true)
    {
        if (IsBaking) return false;
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Activate the building before baking its geometry.", this);
            return false;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying && (!gameObject.scene.IsValid() ||
            PrefabStageUtility.GetPrefabStage(gameObject) != null))
        {
            Debug.LogWarning("Bake a building instance in a scene, outside Prefab Mode.", this);
            return false;
        }
#endif
        var settings = NavMesh.GetSettingsByID(agentTypeID);
        if (settings.agentTypeID == -1)
        {
            Debug.LogError("Choose an existing navigation agent type before baking.", this);
            return false;
        }

        IsBaking = true;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        double collectSeconds = 0;
        try
        {
            var markups = CollectMarkups();
            var buildingSources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(transform, geometryLayers, geometry, 0,
                markups, buildingSources);
            // Nested bakers own their own geometry. Never bake moving agents or obstacles.
            buildingSources.RemoveAll(source => source.area == 1 || (source.component != null &&
                (source.component.GetComponentInParent<BuildingNavMeshBaker>() != this ||
                 source.component.GetComponentInParent<NavMeshAgent>() != null ||
                 source.component.GetComponentInParent<NavMeshObstacle>() != null)));
            if (buildingSources.Count == 0)
            {
                ClearData();
                Debug.LogWarning("No walkable building geometry found. Previous bake cleared. Check Geometry Layers and modifiers.", this);
                return true;
            }

            // Bake only upward-facing walkable surfaces; walls and undersides are excluded.
            settings.agentSlope = Mathf.Min(settings.agentSlope, 60f);
            if (fastBake && !settings.overrideVoxelSize)
            {
                settings.overrideVoxelSize = true;
                settings.voxelSize = Mathf.Max(0.01f, settings.agentRadius / 2f);
            }
            float padding = Mathf.Max(settings.agentHeight, settings.agentRadius) + settings.agentClimb;
            var worldBounds = CalculateBounds(buildingSources, Vector3.zero, Quaternion.identity);
            worldBounds.Expand(2f * padding);
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(worldBounds, geometryLayers | blockingLayers, geometry, 0, markups, sources);
            for (int i = sources.Count - 1; i >= 0; i--)
            {
                var source = sources[i];
                var component = source.component;
                if (component == null || component.GetComponentInParent<NavMeshAgent>() != null)
                {
                    sources.RemoveAt(i);
                    continue;
                }
                bool owned = component.GetComponentInParent<BuildingNavMeshBaker>() == this &&
                    (geometryLayers.value & (1 << component.gameObject.layer)) != 0 &&
                    component.GetComponentInParent<NavMeshObstacle>() == null;
                if (!owned)
                {
                    if ((blockingLayers.value & (1 << component.gameObject.layer)) == 0)
                    {
                        sources.RemoveAt(i);
                        continue;
                    }
                    source.area = 1; // Not Walkable: contributes obstruction without adding navigable tops.
                    sources[i] = source;
                }
            }

            collectSeconds = timer.Elapsed.TotalSeconds;
            PrepareData(force);
            RemoveData();
            for (int i = 0; i < UpDirections.Length; i++)
            {
                var data = ownedData[i];
                // Neighbours must not enlarge the tiled build volume to cover their whole meshes.
                var bounds = CalculateBounds(buildingSources, data.position, data.rotation);
                bounds.Expand(2f * padding);
                // Same native incremental API used by NavMeshSurface.UpdateNavMesh.
                var operation = NavMeshBuilder.UpdateNavMeshDataAsync(data, settings, sources, bounds);
                await Awaitable.FromAsyncOperation(operation);
                if (this == null || destroyed) return false;
            }
            double buildSeconds = timer.Elapsed.TotalSeconds - collectSeconds;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                const string folder = "Assets/GeneratedNavMeshes";
                if (!AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.CreateFolder("Assets", "GeneratedNavMeshes");
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var data in ownedData)
                    {
                        data.hideFlags = HideFlags.None;
                        if (!EditorUtility.IsPersistent(data))
                            AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath($"{folder}/Building-{data.name.Substring(data.name.LastIndexOf('-') + 1)}.asset"));
                        EditorUtility.SetDirty(data);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
                Undo.RecordObject(this, "Bake Building NavMesh");
            }
#endif
            ReleaseTransientData(ownedData);
            bakedData = ownedData;
            bakedAgentTypeID = settings.agentTypeID;
            if (isActiveAndEnabled)
            {
                AddData();
                GenerateLinks(CalculateBounds(buildingSources, Vector3.zero, Quaternion.identity), settings);
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
                if (saveAssets) AssetDatabase.SaveAssets();
            }
#endif
            Debug.Log($"{name}: {sources.Count} sources, collect {collectSeconds:F2}s, " +
                $"Unity bake {buildSeconds:F2}s, save {timer.Elapsed.TotalSeconds - collectSeconds - buildSeconds:F2}s.", this);
            return true;
        }
        catch (Exception exception)
        {
            if (this == null || destroyed) return false;
            ClearData();
            Debug.LogException(exception, this);
            return false;
        }
        finally { IsBaking = false; }
    }

    [ContextMenu("Clear Baked NavMeshes")]
    public void ClearBake()
    {
        if (!IsBaking) ClearData();
    }

    private void ClearData()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RecordObject(this, "Clear Building NavMesh");
#endif
        RemoveData();
        ClearGeneratedLinks();
        ReleaseTransientData();
        bakedData = Array.Empty<NavMeshData>();
        if (ownedData != null)
            foreach (var data in ownedData) DestroyTransient(data);
        ownedData = null;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private void PrepareData(bool force)
    {
        var previousOwned = ownedData;
        bool reuse = ReferenceEquals(ownedData, bakedData);
        var next = new NavMeshData[1];
        for (int i = 0; i < next.Length; i++)
        {
            var old = i < bakedData.Length ? bakedData[i] : null;
            var rotation = transform.rotation * Quaternion.FromToRotation(Vector3.up, UpDirections[i]);
            bool compatible = !force && old != null && bakedAgentTypeID == agentTypeID &&
                old.position == transform.position && Quaternion.Angle(old.rotation, rotation) < .001f;
            // Clone once per owner/session so duplicate buildings never overwrite shared assets.
            next[i] = compatible ? (reuse ? old : Instantiate(old)) : new NavMeshData(agentTypeID);
            next[i].position = transform.position;
            next[i].rotation = rotation;
            next[i].name = $"{name}-{FaceNames[i]}";
            if (!compatible || !reuse) next[i].hideFlags = HideFlags.DontSave;
        }
        ownedData = next;
        if (previousOwned != null && !ReferenceEquals(previousOwned, bakedData))
            foreach (var data in previousOwned) DestroyTransient(data);
    }

    private void AddData()
    {
        RemoveData();
#if UNITY_EDITOR
        if (EditorUtility.IsPersistent(this) || EditorSceneManager.IsPreviewSceneObject(gameObject)) return;
#endif
        // Older scenes may retain five baked faces. Register only the roof.
        for (int i = 0; i < Mathf.Min(1, bakedData.Length); i++)
        {
            var data = bakedData[i];
            if (data == null) continue;
            var instance = NavMesh.AddNavMeshData(data);
            instance.owner = this;
            instances.Add(instance);
        }
        if (generatedLinksRoot != null) generatedLinksRoot.gameObject.SetActive(true);
    }

    private void RemoveData()
    {
        if (generatedLinksRoot != null) generatedLinksRoot.gameObject.SetActive(false);
        foreach (var instance in instances)
            if (instance.valid) instance.Remove();
        instances.Clear();
    }

    private void GenerateLinks(Bounds buildingBounds, NavMeshBuildSettings settings)
    {
        ClearGeneratedLinks();
        Physics.SyncTransforms();
        var mesh = NavMesh.CalculateTriangulation();
        var edges = new Dictionary<(Vector3Int, Vector3Int), (Vector3 a, Vector3 b, Vector3 inside, int count)>();
        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = NavMesh.AllAreas };
        float snapDistance = Mathf.Max(0.2f, settings.agentRadius);
        buildingBounds.Expand(snapDistance * 2f);

        // Keep triangles supported by this building, excluding neighbouring roofs and the ground.
        for (int i = 0; i < mesh.indices.Length; i += 3)
        {
            Vector3 a = mesh.vertices[mesh.indices[i]];
            Vector3 b = mesh.vertices[mesh.indices[i + 1]];
            Vector3 c = mesh.vertices[mesh.indices[i + 2]];
            Vector3 center = (a + b + c) / 3f;
            if (!buildingBounds.Contains(center) ||
                !Physics.Raycast(center + Vector3.up * snapDistance, Vector3.down, out var roof,
                    snapDistance * 2f, geometryLayers, QueryTriggerInteraction.Ignore) ||
                roof.collider.GetComponentInParent<BuildingNavMeshBaker>() != this || roof.normal.y < 0.5f)
                continue;
            AddEdge(a, b, c);
            AddEdge(b, c, a);
            AddEdge(c, a, b);
        }

        var roofEndpoints = new List<Vector3>();
        float spacing = Mathf.Max(1f, linkSpacing);
        foreach (var edge in edges.Values)
        {
            if (edge.count != 1) continue; // Shared triangle edges are inside the roof.
            Vector3 outward = Vector3.Cross(Vector3.up, edge.b - edge.a).normalized;
            if (Vector3.Dot(outward, edge.inside - edge.a) > 0f) outward = -outward;
            int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(edge.a, edge.b) / spacing));
            for (int i = 0; i < samples; i++)
            {
                Vector3 edgePoint = Vector3.Lerp(edge.a, edge.b, (i + 0.5f) / samples);
                if (!NavMesh.SamplePosition(edgePoint - outward * 0.02f, out var upper, snapDistance, filter) ||
                    Mathf.Abs(upper.position.y - edgePoint.y) > snapDistance) continue;
                if (roofEndpoints.Exists(point => (point - upper.position).sqrMagnitude < spacing * spacing * 0.25f))
                    continue;

                // Step past the wall and the ground NavMesh's clearance from it, then look down.
                Vector3 probe = edgePoint + outward * (settings.agentRadius * 3f + 0.5f) + Vector3.up * snapDistance;
                if (!Physics.Raycast(probe, Vector3.down, out var ground, maximumLinkDrop + snapDistance,
                        geometryLayers | blockingLayers, QueryTriggerInteraction.Ignore) || ground.normal.y < 0.5f ||
                    !NavMesh.SamplePosition(ground.point, out var lower, snapDistance, filter) ||
                    Mathf.Abs(lower.position.y - ground.point.y) > snapDistance) continue;

                float drop = upper.position.y - lower.position.y;
                if (drop <= Mathf.Max(0.5f, settings.agentClimb) || drop > maximumLinkDrop) continue;

                // Bring the lower endpoint up to the walkable boundary beside the wall.
                Vector3 lowerEndpoint = lower.position;
                Vector3 towardWall = new Vector3(upper.position.x, lower.position.y, upper.position.z);
                if (NavMesh.Raycast(lower.position, towardWall, out var groundEdge, filter))
                    lowerEndpoint = Vector3.MoveTowards(groundEdge.position, lower.position, 0.02f);

                CreateGeneratedLink(lowerEndpoint, upper.position);
                roofEndpoints.Add(upper.position);
            }
        }
        Debug.Log($"{name}: generated {GeneratedLinkCount} ground/roof links. Ground must be baked and surfaces need colliders.", this);

        void AddEdge(Vector3 a, Vector3 b, Vector3 inside)
        {
            // Weld duplicated vertices at tile boundaries before counting shared edges.
            Vector3Int ka = Vector3Int.RoundToInt(a * 1000f);
            Vector3Int kb = Vector3Int.RoundToInt(b * 1000f);
            var key = (ka, kb);
            if (edges.TryGetValue(key, out var existing))
                edges[key] = (existing.a, existing.b, existing.inside, existing.count + 1);
            else if (edges.TryGetValue((kb, ka), out existing))
                edges[(kb, ka)] = (existing.a, existing.b, existing.inside, existing.count + 1);
            else edges.Add(key, (a, b, inside, 1));
        }
    }

    private void CreateGeneratedLink(Vector3 ground, Vector3 roof)
    {
        if (generatedLinksRoot == null)
            generatedLinksRoot = CreateLinkObject("Generated NavMesh Links", transform).transform;
        var linkObject = CreateLinkObject("Ground to Roof", generatedLinksRoot);
        linkObject.transform.position = ground;
        linkObject.transform.rotation = Quaternion.identity;
        var link = linkObject.AddComponent<NavMeshLink>();
        link.agentTypeID = agentTypeID;
        link.startPoint = Vector3.zero;
        link.endPoint = roof - ground; // NavMeshLink ignores transform scale.
        link.bidirectional = true;
    }

    private static GameObject CreateLinkObject(string objectName, Transform parent)
    {
        var result = new GameObject(objectName);
        result.transform.SetParent(parent, false);
#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(result, "Generate NavMesh Links");
#endif
        return result;
    }

    private void ClearGeneratedLinks()
    {
        if (generatedLinksRoot == null) return;
        var root = generatedLinksRoot.gameObject;
        root.SetActive(false);
#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.DestroyObjectImmediate(root);
        else Destroy(root);
#else
        Destroy(root);
#endif
        generatedLinksRoot = null;
    }

    private static Bounds CalculateBounds(List<NavMeshBuildSource> sources, Vector3 position, Quaternion rotation)
    {
        var worldToBake = Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
        var bounds = new Bounds();
        bool initialized = false;
        foreach (var source in sources)
        {
            var sourceBounds = source.sourceObject is Mesh mesh
                ? mesh.bounds : new Bounds(Vector3.zero, source.size);
            if (source.sourceObject is TerrainData terrain)
                sourceBounds = new Bounds(terrain.size * 0.5f, terrain.size);
            var matrix = worldToBake * source.transform;
            for (int corner = 0; corner < 8; corner++)
            {
                var point = sourceBounds.center + Vector3.Scale(sourceBounds.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1,
                        (corner & 4) == 0 ? -1 : 1));
                point = matrix.MultiplyPoint3x4(point);
                if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; }
                else bounds.Encapsulate(point);
            }
        }
        return bounds;
    }

    private List<NavMeshBuildMarkup> CollectMarkups()
    {
        var markups = new List<NavMeshBuildMarkup>();
        foreach (var modifier in NavMeshModifier.activeModifiers)
        {
            if (!modifier.AffectsAgentType(agentTypeID)) continue;
            markups.Add(new NavMeshBuildMarkup
            {
                root = modifier.transform, overrideArea = modifier.overrideArea,
                area = modifier.area, ignoreFromBuild = modifier.ignoreFromBuild,
                applyToChildren = modifier.applyToChildren
            });
        }
        return markups;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Undo can restore a different serialized bake while this component stays enabled.
        EditorApplication.delayCall += RefreshData;
    }

    private void RefreshData()
    {
        if (this != null && isActiveAndEnabled && !IsBaking) AddData();
    }
#endif

    private void OnDestroy()
    {
        destroyed = true;
        if (ownedData != null)
            foreach (var data in ownedData)
                if (data != null) { NavMeshBuilder.Cancel(data); DestroyTransient(data); }
        ReleaseTransientData();
    }

    private void ReleaseTransientData(NavMeshData[] keep = null)
    {
        foreach (var data in bakedData)
            if (keep == null || Array.IndexOf(keep, data) < 0) DestroyTransient(data);
    }

    private static void DestroyTransient(NavMeshData data)
    {
        if (data == null) return;
#if UNITY_EDITOR
        if (EditorUtility.IsPersistent(data)) return;
        if (!Application.isPlaying) { DestroyImmediate(data); return; }
#else
        // Player builds can share baked assets across instances; only runtime data is disposable.
        // Runtime data is marked DontSave when baked.
        if ((data.hideFlags & HideFlags.DontSave) == 0) return;
#endif
        Destroy(data);
    }
}

