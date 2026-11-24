using System;
using System.IO;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEngine;

public static class EffectsSmokeCheck
{
    private static double nextStep;
    private static int trigger;
    private static int TriggerCount => Enum.GetValues(typeof(GameplayEffect)).Length;
    private static EffectsManager manager;
    private static ParticleSystem[] particles;

    [MenuItem("Tools/Effects/Run Arena Feedback Smoke Check")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Start this check in Edit Mode.");
        SessionState.SetBool("ArenaFeedbackSmokeCheck", true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Register() => EditorApplication.playModeStateChanged += OnPlayMode;

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool("ArenaFeedbackSmokeCheck", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            nextStep = EditorApplication.timeSinceStartup + 2;
            trigger = 0;
            EditorApplication.update += Step;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            EditorApplication.update -= Step;
            SessionState.SetBool("ArenaFeedbackSmokeCheck", false);
        }
    }

    private static void Step()
    {
        if (EditorApplication.timeSinceStartup < nextStep || Time.timeSinceLevelLoad < 1f) return;
        try
        {
            if (!manager)
            {
                manager = UnityEngine.Object.FindAnyObjectByType<EffectsManager>();
                if (!manager || !manager.enabled) throw new Exception("EffectsManager did not initialize.");
                if (manager.GetComponentsInChildren<MMF_Player>(true).Length != TriggerCount)
                    throw new Exception("Expected one configured feedback player per trigger.");
                particles = manager.GetComponentsInChildren<ParticleSystem>(true);
            }
            if (trigger == TriggerCount)
            {
                manager.enabled = false;
                foreach (ParticleSystem system in particles)
                    if (system.particleCount > 0) throw new Exception("Particles remained after disabling effects.");
                manager.enabled = true;
                manager.Play(GameplayEffect.Hit, GameManager.Instance.playerController.transform.position);
                if (manager.transform.Find("Hit").GetComponentInChildren<ParticleSystem>().particleCount == 0)
                    throw new Exception("Feedback did not resume after re-enable.");
                manager.enabled = false;
                File.WriteAllText("/tmp/spell-feel-smoke-result", "PASS: enabled feedbacks emit; disable clears particles; re-enable restores playback.");
                Debug.Log("Arena Feel smoke check passed.");
                EditorApplication.update -= Step;
                EditorApplication.isPlaying = false;
                return;
            }
            SerializedProperty bindings = new SerializedObject(manager).FindProperty("effects");
            bool enabled = false;
            for (int i = 0; i < bindings.arraySize; i++)
            {
                SerializedProperty binding = bindings.GetArrayElementAtIndex(i);
                if (binding.FindPropertyRelative("trigger").enumValueIndex == trigger && binding.FindPropertyRelative("enabled").boolValue)
                    enabled = true;
            }
            if (!enabled) { trigger++; return; }
            var player = GameManager.Instance.playerController;
            Vector3 position = player.transform.position + Vector3.up;
            if ((GameplayEffect)trigger == GameplayEffect.Dodge && Physics.Raycast(player.transform.position + Vector3.up,
                Vector3.down, out RaycastHit ground, 5f, player.groundMask, QueryTriggerInteraction.Ignore))
                position = ground.point + ground.normal * 0.04f;
            manager.Play((GameplayEffect)trigger, position);
            MMF_Player feedback = manager.transform.Find(((GameplayEffect)trigger).ToString()).GetComponent<MMF_Player>();
            foreach (MMF_Feedback effect in feedback.FeedbacksList)
                if (effect is MMF_Particles emission && emission.BoundParticleSystem.particleCount == 0)
                    throw new Exception("No particles from " + (GameplayEffect)trigger + "/" + emission.Label);
            trigger++;
            nextStep = EditorApplication.timeSinceStartup + 1.4;
        }
        catch (Exception exception)
        {
            File.WriteAllText("/tmp/spell-feel-smoke-result", "FAIL: " + exception);
            Debug.LogException(exception);
            EditorApplication.update -= Step;
            EditorApplication.isPlaying = false;
        }
    }
}
