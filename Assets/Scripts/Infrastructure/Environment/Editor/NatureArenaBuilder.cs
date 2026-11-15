using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class NatureArenaBuilder
{
    private const string ScenePath = "Assets/Scenes/Nature Square Arena.unity";
    private const string OutputPath = "Assets/Maps/NatureSquareArena";
    private const string PackPath = "Assets/Polytope Studio/Lowpoly_Environments";
    private const int GroundLayer = 3;

    [MenuItem("Tools/Environment/Create Nature Square Arena")]
    public static void CreateMap()
    {
        if (Application.isPlaying) return;
        if (File.Exists(ScenePath))
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Nature Square Arena already exists; existing scene edits were preserved.");
            return;
        }
        Scene source = SceneManager.GetActiveScene();
        if (UnityEngine.Object.FindAnyObjectByType<PlayerController>() == null)
            throw new InvalidOperationException("Open the gameplay scene with the player before building the arena.");

        AssetDatabase.ImportPackage(PackPath + "/URP/PT_Nature_Free_URP_17.unitypackage", false);
        Directory.CreateDirectory(OutputPath);
        AssetDatabase.Refresh();
        if (!EditorSceneManager.SaveScene(source, ScenePath, true)) return;
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Environment" || root.name == "Enviroment" || root.name == "Cliffstone Basin") root.SetActive(false);
            foreach (NavMeshSurface surface in root.GetComponentsInChildren<NavMeshSurface>(true)) surface.enabled = false;
            foreach (BuildingNavMeshBaker baker in root.GetComponentsInChildren<BuildingNavMeshBaker>(true)) baker.enabled = false;
        }

        Transform map = Group("Nature Square Arena - 64m x 64m", null);
        CreateGround(map);
        Transform fence = Group("01 - Fence and solid limits", map);
        string fencePath = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
        for (int i = 0; i < 16; i++)
        {
            float along = -30f + i * 4f;
            Place(fencePath, fence, new Vector3(along, 0f, -31.5f), 0f, 1f, 4.05f);
            Place(fencePath, fence, new Vector3(along, 0f, 31.5f), 180f, 1f, 4.05f);
            Place(fencePath, fence, new Vector3(-31.5f, 0f, along), 90f, 1f, 4.05f);
            Place(fencePath, fence, new Vector3(31.5f, 0f, along), 90f, 1f, 4.05f);
        }
        AddLimit(fence, new Vector3(-32f, 9f, 0f), new Vector3(1.5f, 24f, 66f));
        AddLimit(fence, new Vector3(32f, 9f, 0f), new Vector3(1.5f, 24f, 66f));
        AddLimit(fence, new Vector3(0f, 9f, -32f), new Vector3(66f, 24f, 1.5f));
        AddLimit(fence, new Vector3(0f, 9f, 32f), new Vector3(66f, 24f, 1.5f));

        var random = new System.Random(7721);
        Transform trees = Group("02 - Border trees", map);
        Transform undergrowth = Group("03 - Shrubs flowers and grass", map);
        for (int side = 0; side < 4; side++)
        {
            for (int i = 0; i < 10; i++)
            {
                float along = -27f + i * 6f + Next(random, -1f, 1f);
                float inset = Next(random, 27f, 29f);
                Vector3 position = side < 2 ? new Vector3(along, 0f, side == 0 ? -inset : inset) : new Vector3(side == 2 ? -inset : inset, 0f, along);
                string tree = i % 3 == 0 ? "PT_Fruit_Tree_01_green" : "PT_Pine_Tree_03_green";
                PlacePrefab("Trees/" + tree, trees, position, Next(random, 0f, 360f), Next(random, 0.8f, 1.15f));
                PlacePrefab("Shrubs/PT_Generic_Shrub_01_green", undergrowth, position + new Vector3(Next(random, -2f, 2f), 0f, Next(random, -2f, 2f)), i * 37f, Next(random, 0.7f, 1.3f));
            }
        }
        Transform cover = Group("04 - Low rock and log cover", map);
        Vector3[] coverPoints = { new Vector3(-16f, 0f, -12f), new Vector3(15f, 0f, -9f), new Vector3(-14f, 0f, 13f), new Vector3(15f, 0f, 15f) };
        for (int i = 0; i < coverPoints.Length; i++)
        {
            Vector3 p = coverPoints[i];
            PlacePrefab("Rocks/PT_Generic_Rock_01", cover, p, i * 73f, 1.25f);
            PlacePrefab("Rocks/PT_River_Rock_Pile_02", cover, p + new Vector3(2f, 0f, 1.5f), i * 51f, 0.8f);
            PlacePrefab("Trees/PT_Pine_Tree_03_logs", cover, p + new Vector3(-3f, 0f, 2f), i * 43f, 1f);
        }
        PlacePrefab("Rocks/PT_Menhir_Rock_02", cover, new Vector3(0f, 0f, 25f), 180f, 1.4f);
        for (int i = 0; i < 110; i++)
        {
            float x = Next(random, -29f, 29f);
            float z = Next(random, -29f, 29f);
            if (Mathf.Abs(x) < 21f && Mathf.Abs(z) < 21f) continue;
            string plant = i % 5 == 0 ? "Flowers/PT_Poppy_02" : "Plants/PT_Grass_02";
            PlacePrefab(plant, undergrowth, new Vector3(x, 0f, z), Next(random, 0f, 360f), Next(random, 0.6f, 1.1f));
        }

        PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        player.transform.SetPositionAndRotation(new Vector3(0f, 1.5f, -21f), Quaternion.identity);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>())
        {
            if (light.type != LightType.Directional) continue;
            light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            light.color = new Color(1f, 0.95f, 0.86f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
        }
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.75f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.47f, 0.37f);
        RenderSettings.ambientGroundColor = new Color(0.23f, 0.27f, 0.21f);
        Physics.SyncTransforms();
        NavMeshSurface navigation = map.gameObject.AddComponent<NavMeshSurface>();
        navigation.collectObjects = CollectObjects.Children;
        navigation.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        navigation.layerMask = (1 << GroundLayer) | 1;
        navigation.BuildNavMesh();
        if (navigation.navMeshData == null) throw new InvalidOperationException("Arena NavMesh bake failed.");
        navigation.navMeshData.name = "Arena Navigation";
        AssetDatabase.CreateAsset(navigation.navMeshData, OutputPath + "/Arena Navigation.asset");
        ValidateMap();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        RenderOverview();
        Selection.activeGameObject = map.gameObject;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.Euler(60f, 0f, 0f), 48f);
        Debug.Log("Nature Square Arena saved: flat 64 x 64 ground, 64 fence sections, four collision limits, Nature Free props and baked navigation.");
    }

    private static Transform Group(string name, Transform parent)
    {
        Transform group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    private static float Next(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private static void CreateGround(Transform map)
    {
        TerrainData data = new TerrainData { name = "Flat Arena Ground", heightmapResolution = 33, alphamapResolution = 256, size = new Vector3(64f, 1f, 64f) };
        AssetDatabase.CreateAsset(data, OutputPath + "/Flat Arena Ground.asset");
        TerrainLayer grass = GroundLayerAsset("Grass", "PT_Ground_Grass_Green_01", 5f);
        TerrainLayer dirt = GroundLayerAsset("Trail", "PT_Ground_Generic_03", 5f);
        data.terrainLayers = new[] { grass, dirt };
        GameObject ground = Terrain.CreateTerrainGameObject(data);
        ground.name = "Flat square ground";
        ground.transform.SetParent(map, false);
        ground.transform.position = new Vector3(-32f, 0f, -32f);
        ground.layer = GroundLayer;
        Material material = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")) { name = "Arena Ground URP" };
        AssetDatabase.CreateAsset(material, OutputPath + "/Arena Ground URP.mat");
        ground.GetComponent<Terrain>().materialTemplate = material;
        PaintGround(data);
    }

    [MenuItem("Tools/Environment/Finish Nature Arena Ground")]
    public static void FinishGround()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().path != ScenePath) return;
        Terrain terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
        PaintGround(terrain.terrainData);
        terrain.Flush();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        RenderOverview();
    }

    private static void PaintGround(TerrainData data)
    {
        var paint = new float[256, 256, 2];
        for (int z = 0; z < 256; z++)
        {
            for (int x = 0; x < 256; x++)
            {
                float wx = x / 255f * 64f - 32f;
                float wz = z / 255f * 64f - 32f;
                float cross = Mathf.Min(Mathf.Abs(wx), Mathf.Abs(wz));
                float loop = Mathf.Abs(Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz)) - 21f);
                float trail = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.6f, 3.5f, Mathf.Min(cross, loop)));
                float clearing = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 8f, new Vector2(wx, wz).magnitude));
                float dirtWeight = Mathf.Max(trail, clearing) * 0.85f;
                paint[z, x, 0] = 1f - dirtWeight;
                paint[z, x, 1] = dirtWeight;
            }
        }
        data.SetAlphamaps(0, 0, paint);
        EditorUtility.SetDirty(data);
        Debug.Log("Arena ground painted; center dirt weight: " + data.GetAlphamaps(128, 128, 1, 1)[0, 0, 1]);
    }

    private static TerrainLayer GroundLayerAsset(string name, string texture, float tileSize)
    {
        TerrainLayer layer = new TerrainLayer
        {
            name = name,
            diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PackPath + "/Sources/Textures/" + texture + ".png"),
            tileSize = new Vector2(tileSize, tileSize),
            smoothness = 0f
        };
        if (layer.diffuseTexture == null) throw new InvalidOperationException("Missing ground texture: " + texture);
        SerializedObject serialized = new SerializedObject(layer);
        serialized.FindProperty("m_SmoothnessSource").intValue = 2;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(layer, OutputPath + "/" + name + ".terrainlayer");
        return layer;
    }

    private static void PlacePrefab(string name, Transform parent, Vector3 position, float yaw, float scale)
    {
        Place(PackPath + "/Prefabs/" + name + ".prefab", parent, position, yaw, scale, 0f);
    }

    private static void Place(string path, Transform parent, Vector3 position, float yaw, float scale, float width)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new InvalidOperationException("Missing Nature Free prefab: " + path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        if (width > 0f)
            instance.transform.localScale = new Vector3(width / bounds.size.x, 1f, 1f);
        else
            instance.transform.localScale *= scale;
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        instance.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        // Decorative meshes stay out of the Ground-layer climbing mesh scanner.
        // Existing prefab colliders still block movement and participate in navigation.
        foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = 0;
            PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
    }

    private static void AddLimit(Transform parent, Vector3 position, Vector3 size)
    {
        Transform wall = Group("Solid arena limit", parent);
        wall.position = position;
        wall.gameObject.layer = GroundLayer;
        wall.gameObject.AddComponent<BoxCollider>().size = size;
    }

    [MenuItem("Tools/Environment/Validate Nature Square Arena")]
    public static void ValidateMap()
    {
        if (SceneManager.GetActiveScene().path != ScenePath) return;
        PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (!NavMesh.SamplePosition(player.transform.position, out NavMeshHit start, 3f, NavMesh.AllAreas))
            throw new InvalidOperationException("Player start has no navigation.");
        for (int i = 0; i < 5; i++)
        {
            Vector3 target = new Vector3(-9f + 2f * i, 0f, -9f);
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(target, out NavMeshHit end, 2f, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException("Enemy spawn is unreachable: " + i);
        }
        foreach (Vector3 direction in new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
        {
            if (!Physics.Raycast(new Vector3(0f, 5f, 0f), direction, 33f, 1 << GroundLayer))
                throw new InvalidOperationException("An arena boundary is missing.");
        }
        Debug.Log("Nature Square Arena validation passed: all five spawns reachable and all four limits block raycasts.");
    }

    [MenuItem("Tools/Environment/Apply Woodland Color Style")]
    public static void ApplyWoodlandStyle()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().path != ScenePath) return;
        Transform map = GameObject.Find("Nature Square Arena - 64m x 64m").transform;
        string palettePath = OutputPath + "/Palette";
        Directory.CreateDirectory(palettePath);
        AssetDatabase.Refresh();
        var variants = new Dictionary<Material, Material>();
        foreach (Renderer renderer in map.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material original = materials[i];
                if (original == null || AssetDatabase.GetAssetPath(original).StartsWith(palettePath)) continue;
                if (!variants.TryGetValue(original, out Material variant))
                {
                    string path = palettePath + "/" + original.name + " Woodland.mat";
                    variant = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (variant == null)
                    {
                        variant = new Material(original) { name = original.name + " Woodland" };
                        AssetDatabase.CreateAsset(variant, path);
                    }
                    string name = original.name.ToLowerInvariant();
                    bool foliage = name.Contains("leav") || name.Contains("foliage") || name.Contains("grass") || name.Contains("leaf");
                    if (foliage && variant.HasProperty("_CUSTOMCOLORSTINTING"))
                    {
                        variant.SetFloat("_CUSTOMCOLORSTINTING", 1f);
                        variant.EnableKeyword("_CUSTOMCOLORSTINTING_ON");
                        variant.SetColor("_TopColor", new Color(0.22f, 0.32f, 0.14f));
                        variant.SetColor("_GroundColor", new Color(0.08f, 0.14f, 0.07f));
                    }
                    if (name.Contains("rock") && variant.HasProperty("_GRADIENTONOFF"))
                    {
                        variant.SetFloat("_GRADIENTONOFF", 1f);
                        variant.EnableKeyword("_GRADIENTONOFF_ON");
                        variant.SetColor("_TopColor", new Color(0.65f, 0.61f, 0.5f));
                        variant.SetColor("_GroundColor", new Color(0.35f, 0.38f, 0.3f));
                    }
                    if (variant.HasProperty("_Smoothness")) variant.SetFloat("_Smoothness", 0.15f);
                    EditorUtility.SetDirty(variant);
                    variants.Add(original, variant);
                }
                materials[i] = variant;
            }
            renderer.sharedMaterials = materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        foreach (TerrainLayer layer in UnityEngine.Object.FindAnyObjectByType<Terrain>().terrainData.terrainLayers)
        {
            layer.diffuseRemapMin = Vector4.zero;
            layer.diffuseRemapMax = layer.name == "Grass" ? new Vector4(0.82f, 0.67f, 0.58f, 1f) : new Vector4(0.8f, 0.76f, 0.66f, 1f);
            EditorUtility.SetDirty(layer);
        }
        TintCharacter("Assets/Materials/Characters/Materials/Hooded Shadowwalker.mat", new Color(0.66f, 0.73f, 0.68f), 0.25f);
        TintCharacter("Assets/Materials/Characters/Materials/Crimson Wraith.mat", new Color(0.82f, 0.65f, 0.57f), 0.3f);

        string profilePath = palettePath + "/Woodland Grade.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Woodland Grade";
            AssetDatabase.CreateAsset(profile, profilePath);
            ColorAdjustments colors = profile.Add<ColorAdjustments>();
            colors.saturation.Override(-22f);
            colors.contrast.Override(14f);
            colors.colorFilter.Override(new Color(1f, 0.98f, 0.94f));
            AssetDatabase.AddObjectToAsset(colors, profile);
            WhiteBalance balance = profile.Add<WhiteBalance>();
            balance.temperature.Override(5f);
            AssetDatabase.AddObjectToAsset(balance, profile);
        }
        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>();
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        bloom.threshold.Override(0.65f);
        bloom.intensity.Override(0.65f);
        bloom.scatter.Override(0.55f);
        bloom.highQualityFiltering.Override(false);
        bloom.maxIterations.Override(4);
        EditorUtility.SetDirty(bloom);
        if (!profile.TryGet(out Vignette vignette))
        {
            vignette = profile.Add<Vignette>();
            AssetDatabase.AddObjectToAsset(vignette, profile);
        }
        vignette.intensity.Override(0.3f);
        vignette.smoothness.Override(0.5f);
        EditorUtility.SetDirty(vignette);
        Transform grade = map.Find("05 - Woodland color grade");
        if (grade == null) grade = Group("05 - Woodland color grade", map);
        Volume volume = grade.GetComponent<Volume>();
        if (volume == null) volume = grade.gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
        foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>())
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        EditorUtility.SetDirty(profile);
        FinishGround();
        Debug.Log("Woodland palette applied: moss foliage, warm stone, charcoal/olive player, rust-red enemy and shared low-saturation grading.");
    }

    private static void TintCharacter(string path, Color tint, float smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) throw new InvalidOperationException("Missing character material: " + path);
        material.SetColor("_BaseColor", tint);
        material.SetColor("_Color", tint);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0.25f);
        EditorUtility.SetDirty(material);
    }

    [MenuItem("Tools/Environment/Configure Arena Camera")]
    public static void ConfigureArenaCamera()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().path != ScenePath) return;
        CinemachineCamera camera = UnityEngine.Object.FindAnyObjectByType<CinemachineCamera>();
        PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (camera == null || player == null) throw new InvalidOperationException("Arena needs its Cinemachine camera and player.");
        CinemachineOrbitalFollow orbit = camera.GetComponent<CinemachineOrbitalFollow>();
        CinemachineRotationComposer composition = camera.GetComponent<CinemachineRotationComposer>();
        camera.Follow = player.transform;
        camera.LookAt = player.transform;
        camera.Lens.FieldOfView = 45f;
        orbit.TargetOffset = new Vector3(0f, 1f, 0f);
        orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
        orbit.Radius = 14f;
        orbit.TrackerSettings.BindingMode = BindingMode.WorldSpace;
        orbit.TrackerSettings.PositionDamping = new Vector3(0.25f, 0.15f, 0.25f);
        orbit.HorizontalAxis.Value = 0f;
        orbit.HorizontalAxis.Center = 0f;
        orbit.HorizontalAxis.Recentering.Enabled = false;
        orbit.VerticalAxis.Value = 55f;
        orbit.VerticalAxis.Center = 55f;
        orbit.VerticalAxis.Range = new Vector2(55f, 55f);
        orbit.VerticalAxis.Recentering.Enabled = false;
        composition.TargetOffset = new Vector3(0f, 1f, 0f);
        composition.Damping = new Vector2(0.15f, 0.15f);
        composition.Composition.ScreenPosition = Vector2.zero;
        CinemachineInputAxisController oldInput = camera.GetComponent<CinemachineInputAxisController>();
        if (oldInput != null) oldInput.enabled = false;
        if (camera.GetComponent<ArenaCameraInput>() == null) camera.gameObject.AddComponent<ArenaCameraInput>();
        camera.name = "Arena Camera";
        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(orbit);
        EditorUtility.SetDirty(composition);
        FinishGround();
        Debug.Log("Arena camera configured: 55 degree pitch, 14m distance, 45 degree FOV; middle-drag orbit and scroll zoom from 14m to 24m.");
    }

    [MenuItem("Tools/Environment/Render Nature Square Overview")]
    public static void RenderOverview()
    {
        GameObject temporary = new GameObject("Temporary nature overview");
        Camera camera = temporary.AddComponent<Camera>();
        camera.transform.position = new Vector3(51f, 67f, -62f);
        camera.transform.LookAt(new Vector3(0f, 0f, 0f));
        camera.fieldOfView = 50f;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camera.backgroundColor = new Color(0.23f, 0.32f, 0.37f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        var target = new RenderTexture(1500, 1200, 24);
        var image = new Texture2D(1500, 1200, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1500, 1200), 0, 0);
            image.Apply();
            Directory.CreateDirectory("GeneratedAssets/NatureSquareArena");
            File.WriteAllBytes("GeneratedAssets/NatureSquareArena/Overview.png", image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(temporary);
        }
    }
}
