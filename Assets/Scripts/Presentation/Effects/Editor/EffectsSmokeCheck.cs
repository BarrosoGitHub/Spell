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
        if (EditorApplication.timeSinceStartup < nextStep) return;
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
                File.WriteAllText("/tmp/spell-feel-smoke-result", "PASS: all feedbacks emit; disable clears particles; re-enable restores playback.");
                Debug.Log("Arena Feel smoke check passed.");
                EditorApplication.update -= Step;
                EditorApplication.isPlaying = false;
                return;
            }
            manager.Play((GameplayEffect)trigger, GameManager.Instance.playerController.transform.position + Vector3.up);
            ParticleSystem emitted = manager.transform.Find(((GameplayEffect)trigger).ToString())
                .GetComponentInChildren<ParticleSystem>();
            if (emitted.particleCount == 0) throw new Exception("No particles from " + (GameplayEffect)trigger);
            trigger++;
            nextStep = EditorApplication.timeSinceStartup + 0.1;
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
