using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MobTest))]
public sealed class MobTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MobTest mission = (MobTest)target;
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !mission.isActiveAndEnabled ||
                   mission.State != mission.RunningState))
        {
            if (GUILayout.Button("Spawn Mob")) mission.SpawnMob();
            if (GUILayout.Button("Clear Test Mobs")) mission.ClearMobs();
        }
        if (!Application.isPlaying || mission.State != mission.RunningState)
            EditorGUILayout.HelpBox("Select MobTest in MissionManager and start the mission in Play Mode to spawn mobs.", MessageType.Info);
    }
}
