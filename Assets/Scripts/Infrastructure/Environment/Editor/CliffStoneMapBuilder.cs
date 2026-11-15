using System;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class CliffStoneMapBuilder
{
    private const string ScenePath = "Assets/Scenes/Cliffstone Basin.unity";
    private const string OutputPath = "Assets/Maps/CliffstoneBasin";
    private const string PackPath = "Assets/Dzeruza/CliffStonePackL";
    private const float MapSize = 96f;
    private const int GroundLayer = 3;

    [MenuItem("Tools/Environment/Create Cliffstone Basin")]
    public static void CreateMap()
    {
        if (Application.isPlaying) return;
        if (File.Exists(ScenePath))
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Cliffstone Basin already exists. Open the scene to edit it; existing edits were preserved.");
            return;
        }

        Scene source = SceneManager.GetActiveScene();
        if (source.name != "Test 1")
            throw new InvalidOperationException("Open Test 1 before building the map so its player and managers are preserved.");

        Directory.CreateDirectory(OutputPath);
        AssetDatabase.Refresh();
        // Save a copy of the live scene, including the user's current scene edits.
        if (!EditorSceneManager.SaveScene(source, ScenePath, true)) return;
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Enviroment" || root.name == "Environment") root.SetActive(false);
            foreach (NavMeshSurface oldSurface in root.GetComponentsInChildren<NavMeshSurface>(true))
                oldSurface.enabled = false;
            foreach (BuildingNavMeshBaker baker in root.GetComponentsInChildren<BuildingNavMeshBaker>(true))
                baker.enabled = false;
        }

        Transform map = new GameObject("Cliffstone Basin").transform;
        Material moss = CreateRockMaterial("Mossy", new Color(0.88f, 0.94f, 0.85f));
        Material grey = CreateRockMaterial("Grey", new Color(0.88f, 0.91f, 0.94f));
        CreateTerrain(map);
        Transform cliffs = Group("01 - Canyon walls", map);
        Transform cover = Group("02 - Rock formations and cover", map);
        Transform scatter = Group("03 - Scree", map);
        var random = new System.Random(9241);

        for (int i = 0; i < 36; i++)
        {
            float angle = i * Mathf.PI * 2f / 36f;
            float x = Mathf.Cos(angle) * (35f + Next(random, -1.5f, 1.5f));
            float z = Mathf.Sin(angle) * (38f + Next(random, -1.5f, 1.5f));
            AddRock(cliffs, i % 4 + 1, new Vector3(x, Height(x, z) - 1.4f, z),
                new Vector3(Next(random, 10f, 14f), Next(random, 8f, 15f), Next(random, 7f, 10f)),
                -angle * Mathf.Rad2Deg + Next(random, -24f, 24f), i % 4 == 0 ? grey : moss);
        }

        AddRock(cover, 3, new Vector3(6f, -1f, 29f), new Vector3(9f, 19f, 8f), 24f, grey);
        AddRock(cover, 1, new Vector3(12f, -0.5f, 28f), new Vector3(8f, 11f, 7f), -28f, moss);
        Vector3[] islands =
        {
            new Vector3(-14f, 0f, -10f), new Vector3(13f, 0f, -5f),
            new Vector3(-13f, 0f, 12f), new Vector3(15f, 0f, 15f),
            new Vector3(-23f, 0f, 2f), new Vector3(24f, 0f, -20f)
        };
        for (int i = 0; i < islands.Length; i++)
        {
            Vector3 p = islands[i];
            p.y = Height(p.x, p.z) - 0.3f;
            AddRock(cover, 5 + i % 5, p, new Vector3(6f, 2.2f + i % 3, 4.5f), i * 57f, moss);
            Vector3 foot = p + new Vector3(3f, 0f, 2f);
            foot.y = Height(foot.x, foot.z) - 0.15f;
            AddRock(cover, 8, foot, new Vector3(3.5f, 0.9f, 2.5f), i * 31f, grey);
        }

        for (int i = 0; i < 75; i++)
        {
            float angle = Next(random, 0f, Mathf.PI * 2f);
            float radius = Next(random, 0.76f, 0.91f);
            float x = Mathf.Cos(angle) * 35f * radius;
            float z = Mathf.Sin(angle) * 38f * radius;
            float size = Next(random, 0.4f, 1.8f);
            AddRock(scatter, 5 + i % 6, new Vector3(x, Height(x, z) - 0.12f, z),
                new Vector3(size * 1.5f, size, size), Next(random, 0f, 360f), i % 3 == 0 ? grey : moss);
        }

        Transform boundary = Group("04 - Safety boundary", map);
        AddBoundary(boundary, new Vector3(-43f, 12f, 0f), new Vector3(2f, 40f, 96f));
        AddBoundary(boundary, new Vector3(43f, 12f, 0f), new Vector3(2f, 40f, 96f));
        AddBoundary(boundary, new Vector3(0f, 12f, -45f), new Vector3(96f, 40f, 2f));
        AddBoundary(boundary, new Vector3(0f, 12f, 45f), new Vector3(96f, 40f, 2f));

        PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (player == null) throw new InvalidOperationException("The source scene needs its player.");
        player.transform.position = new Vector3(0f, Height(0f, -22f) + 2f, -22f);
        player.transform.rotation = Quaternion.identity;
        Transform markers = Group("05 - Encounter markers", map);
        Group("Player start", markers).position = player.transform.position;
        Group("Combat clearing", markers).position = Vector3.zero;
        Group("North overlook", markers).position = new Vector3(-14f, Height(-14f, 18f), 18f);
        ConfigureLighting();

        Physics.SyncTransforms();
        NavMeshSurface surface = map.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = 1 << GroundLayer;
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new InvalidOperationException("Map navigation bake failed.");
        AssetDatabase.CreateAsset(surface.navMeshData, OutputPath + "/Basin Navigation.asset");
        ValidateNavigation(player.transform.position);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        RenderOverview();
        Selection.activeGameObject = map.gameObject;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.LookAt(new Vector3(0f, 2f, 0f), Quaternion.Euler(48f, 0f, 0f), 65f);
        Debug.Log("Cliffstone Basin created: playable terrain, 125 pack rock instances, URP materials, baked navigation and retained gameplay setup.");
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

    private static float Height(float x, float z)
    {
        float edge = Mathf.Max(Mathf.Abs(x) - 29f, Mathf.Abs(z) - 33f);
        float rim = Mathf.SmoothStep(0f, 9f, Mathf.Clamp01(edge / 15f));
        float westRise = 3.6f * Mathf.Exp(-((x + 18f) * (x + 18f) / 85f + (z - 17f) * (z - 17f) / 140f));
        float eastRise = 1.8f * Mathf.Exp(-((x - 23f) * (x - 23f) / 60f + (z - 5f) * (z - 5f) / 200f));
        float noise = (Mathf.PerlinNoise(x * 0.085f + 20f, z * 0.085f + 20f) - 0.5f) * 0.45f;
        float clearing = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((new Vector2(x, z).magnitude - 12f) / 10f));
        return Mathf.Max(0f, rim + (westRise + eastRise + noise) * clearing);
    }

    private static Terrain CreateTerrain(Transform parent)
    {
        TerrainData data = new TerrainData { name = "Basin Terrain", heightmapResolution = 257, alphamapResolution = 256, size = new Vector3(MapSize, 16f, MapSize) };
        var heights = new float[257, 257];
        for (int z = 0; z < 257; z++)
            for (int x = 0; x < 257; x++)
                heights[z, x] = Height(x / 256f * MapSize - 48f, z / 256f * MapSize - 48f) / 16f;
        data.SetHeights(0, 0, heights);
        data.terrainLayers = new[]
        {
            CreateLayer("GrassyMud", "Grassy_Mud", 7f),
            CreateLayer("Grass", "Grass", 9f),
            CreateLayer("Mud", "Mud", 5f)
        };
        var weights = new float[256, 256, 3];
        for (int z = 0; z < 256; z++)
        {
            for (int x = 0; x < 256; x++)
            {
                float wx = x / 255f * MapSize - 48f;
                float wz = z / 255f * MapSize - 48f;
                float noise = Mathf.PerlinNoise(wx * 0.11f + 30f, wz * 0.11f + 30f);
                float route = Mathf.Exp(-Mathf.Pow((wx - Mathf.Sin(wz * 0.12f) * 4f) / 3.8f, 2f));
                float ring = Mathf.Exp(-Mathf.Pow((new Vector2(wx, wz).magnitude - 21f) / 2.8f, 2f));
                float mud = Mathf.Clamp01(Mathf.Max(route, ring) * 0.82f + noise * 0.15f);
                float grass = (1f - mud) * Mathf.SmoothStep(0.15f, 0.9f, noise);
                weights[z, x, 0] = 1f - mud - grass;
                weights[z, x, 1] = grass;
                weights[z, x, 2] = mud;
            }
        }
        data.SetAlphamaps(0, 0, weights);
        AssetDatabase.CreateAsset(data, OutputPath + "/Basin Terrain.asset");
        GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
        terrainObject.name = "Painted ground - clearing and flank trails";
        terrainObject.transform.SetParent(parent, false);
        terrainObject.transform.position = new Vector3(-48f, 0f, -48f);
        terrainObject.layer = GroundLayer;
        Terrain terrain = terrainObject.GetComponent<Terrain>();
        var material = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")) { name = "Basin Ground URP" };
        AssetDatabase.CreateAsset(material, OutputPath + "/Basin Ground URP.mat");
        terrain.materialTemplate = material;
        terrain.drawInstanced = true;
        return terrain;
    }

    private static TerrainLayer CreateLayer(string folder, string texture, float tileSize)
    {
        string path = PackPath + "/Textures&Materials/Terrain/" + folder + "/T_" + texture;
        TerrainLayer layer = new TerrainLayer
        {
            name = folder,
            diffuseTexture = Load<Texture2D>(path + "_Basecolor.png"),
            normalMapTexture = Load<Texture2D>(path + (folder == "Mud" ? "_Normal_OpenGL.png" : "_Normal.png")),
            tileSize = new Vector2(tileSize, tileSize),
            smoothness = 0.08f,
            normalScale = 0.65f
        };
        AssetDatabase.CreateAsset(layer, OutputPath + "/" + folder + ".terrainlayer");
        return layer;
    }

    private static Material CreateRockMaterial(string variant, Color tint)
    {
        string texturePath = PackPath + "/Textures&Materials/StonesSemiRealistic/T_Cliffs_L_SR_";
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Cliff " + variant + " URP", enableInstancing = true };
        material.SetTexture("_BaseMap", Load<Texture2D>(texturePath + variant + "_BaseMap.png"));
        material.SetTexture("_BumpMap", Load<Texture2D>(texturePath + "Normal.png"));
        material.SetTexture("_OcclusionMap", Load<Texture2D>(texturePath + "AO.png"));
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Smoothness", 0.12f);
        material.SetFloat("_BumpScale", 0.8f);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_OCCLUSIONMAP");
        AssetDatabase.CreateAsset(material, OutputPath + "/" + material.name + ".mat");
        return material;
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException("Missing map asset: " + path);
        return asset;
    }

    private static void AddRock(Transform parent, int model, Vector3 position, Vector3 size, float yaw, Material material)
    {
        string modelName = (model < 5 ? "SM_Cliff_L" : "SM_CliffStone_L") + model + "_SR_Mossy Variant.prefab";
        GameObject prefab = Load<GameObject>(PackPath + "/3DModels/Prefabs/Stones/SemiRealisticMossy/" + modelName);
        GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        rock.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Renderer[] renderers = rock.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
        rock.transform.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        rock.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (Transform child in rock.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = GroundLayer;
        if (rock.GetComponentsInChildren<Collider>(true).Length == 0)
        {
            LODGroup lod = rock.GetComponentInChildren<LODGroup>();
            Renderer collisionRenderer = lod != null && lod.GetLODs().Length > 1 ? lod.GetLODs()[1].renderers[0] : renderers[0];
            MeshFilter filter = collisionRenderer.GetComponent<MeshFilter>();
            if (filter != null) collisionRenderer.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(rock.transform);
        foreach (Renderer renderer in renderers) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }

    private static void AddBoundary(Transform parent, Vector3 position, Vector3 size)
    {
        Transform wall = Group("Boundary", parent);
        wall.position = position;
        wall.gameObject.layer = GroundLayer;
        wall.gameObject.AddComponent<BoxCollider>().size = size;
    }

    private static void ConfigureLighting()
    {
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>())
        {
            if (light.type != LightType.Directional) continue;
            light.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            light.color = new Color(1f, 0.91f, 0.78f);
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.48f, 0.59f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.35f, 0.39f, 0.32f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.22f, 0.2f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.55f, 0.65f, 0.68f);
        RenderSettings.fogDensity = 0.004f;
    }

    private static void ValidateNavigation(Vector3 start)
    {
        if (!NavMesh.SamplePosition(start, out NavMeshHit from, 5f, NavMesh.AllAreas))
            throw new InvalidOperationException("Player start is not on the map NavMesh.");
        for (int i = 0; i < 5; i++)
        {
            Vector3 target = new Vector3(-9f + i * 2f, 0f, -9f);
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(target, out NavMeshHit to, 2f, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException("Exterminate spawn " + i + " is not reachable from the player start.");
        }
        Debug.Log("Cliffstone Basin navigation: all five Exterminate spawn positions are reachable from player start.");
    }

    [MenuItem("Tools/Environment/Finish Basin Landmarks")]
    public static void FinishLandmarks()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().path != ScenePath) return;
        Transform map = GameObject.Find("Cliffstone Basin").transform;
        if (map.Find("06 - Overlook and stone arch") != null) return;
        Transform landmarks = Group("06 - Overlook and stone arch", map);
        Material moss = Load<Material>(OutputPath + "/Cliff Mossy URP.mat");
        Material grey = Load<Material>(OutputPath + "/Cliff Grey URP.mat");
        // Two uneven buttresses and an embedded lintel frame the northern approach.
        AddRock(landmarks, 2, new Vector3(-8f, 0f, 23f), new Vector3(6f, 9f, 7f), -15f, moss);
        AddRock(landmarks, 4, new Vector3(3f, 0f, 23f), new Vector3(6f, 10f, 7f), 18f, grey);
        AddRock(landmarks, 7, new Vector3(-2.5f, 7f, 23f), new Vector3(16f, 3.5f, 6.5f), 5f, moss);
        AddRock(landmarks, 4, new Vector3(-24f, 1.5f, 17f), new Vector3(10f, 6f, 12f), 40f, moss);
        AddRock(landmarks, 6, new Vector3(-18f, 2f, 22f), new Vector3(9f, 2.8f, 8f), -20f, grey);
        AddRock(landmarks, 1, new Vector3(26f, 0.5f, 9f), new Vector3(9f, 7f, 11f), -25f, moss);
        AddRock(landmarks, 5, new Vector3(20f, 0f, 7f), new Vector3(6f, 2.6f, 7f), 20f, moss);
        foreach (TerrainLayer layer in UnityEngine.Object.FindAnyObjectByType<Terrain>().terrainData.terrainLayers)
        {
            layer.diffuseRemapMin = Vector4.zero;
            layer.diffuseRemapMax = layer.name == "Grass" ? new Vector4(0.62f, 0.68f, 0.53f, 1f) : new Vector4(0.8f, 0.82f, 0.75f, 1f);
            EditorUtility.SetDirty(layer);
        }
        Physics.SyncTransforms();
        NavMeshSurface surface = map.GetComponent<NavMeshSurface>();
        NavMeshData saved = surface.navMeshData;
        surface.BuildNavMesh();
        NavMeshData rebuilt = surface.navMeshData;
        surface.RemoveData();
        EditorUtility.CopySerialized(rebuilt, saved);
        surface.navMeshData = saved;
        surface.AddData();
        UnityEngine.Object.DestroyImmediate(rebuilt);
        EditorUtility.SetDirty(saved);
        ValidateNavigation(UnityEngine.Object.FindAnyObjectByType<PlayerController>().transform.position);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        RenderOverview();
        Debug.Log("Cliffstone Basin landmarks finished and navigation revalidated.");
    }

    [MenuItem("Tools/Environment/Render Cliffstone Overview")]
    public static void RenderOverview()
    {
        GameObject cameraObject = new GameObject("Temporary map overview");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(58f, 66f, -74f);
        camera.transform.LookAt(new Vector3(0f, 2f, 1f));
        camera.fieldOfView = 52f;
        camera.farClipPlane = 250f;
        RenderTexture target = new RenderTexture(1600, 1100, 24);
        RenderTexture previous = RenderTexture.active;
        Texture2D image = new Texture2D(1600, 1100, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1600, 1100), 0, 0);
            image.Apply();
            Directory.CreateDirectory("GeneratedAssets/CliffstoneBasin");
            File.WriteAllBytes("GeneratedAssets/CliffstoneBasin/Overview.png", image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
