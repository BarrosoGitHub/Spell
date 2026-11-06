using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MissionManager))]
public sealed class MissionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MissionManager manager = (MissionManager)target;
        string[] labels = Array.ConvertAll(manager.AvailableMissions, type => type.Name);
        int selectedIndex = manager.selectedMission == null
            ? -1
            : Array.IndexOf(manager.AvailableMissions, manager.selectedMission.GetType());

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            int index = EditorGUILayout.Popup("Mission Type", selectedIndex, labels);
            if (index != selectedIndex)
                manager.SelectMission(index);

            using (new EditorGUI.DisabledScope(manager.selectedMission == null))
            {
                Mission mission = manager.selectedMission;
                bool running = mission != null && mission.State == mission.RunningState;

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(mission != null && mission.State != mission.ReadyState))
                        if (GUILayout.Button("Start Mission")) manager.StartMission();
                    using (new EditorGUI.DisabledScope(running))
                        if (GUILayout.Button("Reset")) manager.ResetMission();
                }

                using (new EditorGUI.DisabledScope(!running))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Complete")) manager.CompleteMission();
                    if (GUILayout.Button("Failed")) manager.FailMission();
                    if (GUILayout.Button("End Mission")) manager.AbortMission();
                }
            }
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Enter Play Mode to select and control a mission.", MessageType.Info);
    }
}
