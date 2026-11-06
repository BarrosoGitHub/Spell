using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[CustomEditor(typeof(BuildingNavMeshBaker)), CanEditMultipleObjects]
public sealed class BuildingNavMeshBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        bool busy = System.Array.Exists(targets, item => ((BuildingNavMeshBaker)item).IsBaking);
        using (new EditorGUI.DisabledScope(busy))
        {
            serializedObject.Update();
            var agent = serializedObject.FindProperty("agentTypeID");
            int count = NavMesh.GetSettingsCount();
            var names = new string[count];
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = NavMesh.GetSettingsByIndex(i).agentTypeID;
                names[i] = NavMesh.GetSettingsNameFromID(ids[i]);
            }
            EditorGUI.showMixedValue = agent.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int chosen = EditorGUILayout.IntPopup("Agent Type", agent.intValue, names, ids);
            if (EditorGUI.EndChangeCheck()) agent.intValue = chosen;
            EditorGUI.showMixedValue = false;
            DrawPropertiesExcluding(serializedObject, "m_Script", "agentTypeID");
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("Unity bakes asynchronously and reuses unchanged tiles. Geometry Layers select roof geometry; Blocking Layers include nearby obstructions. Mark invalid child objects Not Walkable with a NavMeshModifier.", MessageType.Info);
            if (GUILayout.Button("Bake Roof"))
                foreach (BuildingNavMeshBaker baker in targets) baker.Bake();
            if (GUILayout.Button("Clear Bake"))
                foreach (BuildingNavMeshBaker baker in targets) baker.ClearBake();
            if (GUILayout.Button("Force Rebuild"))
                foreach (BuildingNavMeshBaker baker in targets) baker.ForceRebuild();

            EditorGUILayout.HelpBox("Roof baking automatically generates links to lower NavMeshes. Bake the ground first and give the building and ground colliders. Link Spacing controls density; Maximum Link Drop controls height.", MessageType.Info);
            if (targets.Length == 1)
                EditorGUILayout.LabelField("Generated Links", ((BuildingNavMeshBaker)target).GeneratedLinkCount.ToString());

        }
        if (busy) { EditorGUILayout.LabelField("Unity is baking…"); Repaint(); }
    }
}

[CustomEditor(typeof(CityNavMeshBaker))]
public sealed class CityNavMeshBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var city = (CityNavMeshBaker)target;
        EditorGUILayout.HelpBox("Uses Unity's asynchronous, incremental baker, one building at a time. Disabled bakers are enabled; inactive buildings are skipped.", MessageType.Info);
        using (new EditorGUI.DisabledScope(city.IsBaking))
        {
            if (GUILayout.Button("Bake All Buildings"))
                ((CityNavMeshBaker)target).BakeAll();
            if (GUILayout.Button("Force Rebuild All"))
                ((CityNavMeshBaker)target).ForceRebuildAll();
        }
        if (!string.IsNullOrEmpty(city.Status)) EditorGUILayout.LabelField(city.Status);
        if (city.IsBaking)
        {
            if (GUILayout.Button("Stop After Current Building")) city.StopAfterCurrentBuilding();
            Repaint();
        }
    }
}

