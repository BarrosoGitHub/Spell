using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Scene-local pool for visual hitscan tracers and scenery impacts.</summary>
public sealed class BulletShotEffects : MonoBehaviour
{
    private const int MaxEffects = 128;
    private static BulletShotEffects instance;
    private readonly List<Shot> active = new List<Shot>();
    private readonly Stack<Shot> available = new Stack<Shot>();
    private Material defaultMaterial;
    private int created;

    private sealed class Shot
    {
        public GameObject Root;
        public ParticleSystem Tracer;
        public ParticleSystemRenderer TracerRenderer;
        public ParticleSystem Sparks;
        public ParticleSystem Dust;
        public Vector3 Origin, End, Normal, Direction;
        public float Distance, Duration, Length, Age;
        public int SpawnFrame;
        public bool WallHit, Arrived;
    }

    public static void Play(Vector3 origin, Vector3 end, Vector3 normal, bool wallHit,
        float speed, float length, float width, Color color, Material material = null)
    {
        if (instance == null)
            instance = new GameObject("Bullet Shot Effects").AddComponent<BulletShotEffects>();

        instance.Spawn(origin, end, normal, wallHit, speed, length, width, color, material);
    }

    private void Awake()
    {
        defaultMaterial = Resources.Load<Material>("WeaponShotEffect");
    }

    private void Spawn(Vector3 origin, Vector3 end, Vector3 normal, bool wallHit,
        float speed, float length, float width, Color color, Material material)
    {
        material = material != null ? material : defaultMaterial;
        if (material == null || (available.Count == 0 && created >= MaxEffects))
            return;

        Shot shot = available.Count > 0 ? available.Pop() : CreateShot();
        shot.Origin = origin;
        shot.End = end;
        shot.Distance = Vector3.Distance(origin, end);
        shot.Direction = (end - origin).normalized;
        shot.Normal = normal;
        shot.WallHit = wallHit;
        shot.Length = Mathf.Max(0.01f, length);
        shot.Duration = Mathf.Max(0.035f, shot.Distance / Mathf.Max(1f, speed));
        shot.Age = 0f;
        shot.Arrived = false;
        shot.SpawnFrame = Time.frameCount;
        shot.Root.SetActive(true);
        float particleWidth = Mathf.Max(0.001f, width);
        shot.TracerRenderer.sharedMaterial = material;
        shot.TracerRenderer.lengthScale = Mathf.Min(shot.Length, shot.Distance) / particleWidth;
        shot.Tracer.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        shot.Tracer.Play(false);
        if (shot.Distance > 0.001f)
        {
            shot.Tracer.Emit(new ParticleSystem.EmitParams
            {
                position = origin,
                velocity = shot.Direction * (shot.Distance / shot.Duration),
                startLifetime = shot.Duration + 0.1f,
                startSize = particleWidth,
                startColor = color
            }, 1);
        }
        // Advance this system explicitly with the arrival timer, so a long frame
        // cannot move the bullet beyond the raycast surface before the impact.
        shot.Tracer.Pause(false);
        shot.Sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        shot.Dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        active.Add(shot);
    }

    private Shot CreateShot()
    {
        created++;
        var root = new GameObject("Pooled bullet tracer and impact");
        root.transform.SetParent(transform, false);
        var tracer = CreateParticles(root.transform, "Bullet travel", false);
        var main = tracer.main;
        main.gravityModifier = 0f;
        main.maxParticles = 1;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var fade = tracer.colorOverLifetime;
        fade.enabled = false;
        var size = tracer.sizeOverLifetime;
        size.enabled = false;
        var renderer = tracer.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0f;
        renderer.cameraVelocityScale = 0f;
        return new Shot
        {
            Root = root,
            Tracer = tracer,
            TracerRenderer = renderer,
            Sparks = CreateParticles(root.transform, "Impact sparks", false),
            Dust = CreateParticles(root.transform, "Impact dust", true)
        };
    }

    private static ParticleSystem CreateParticles(Transform parent, string name, bool dust)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var particles = obj.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.None;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 32;
        main.gravityModifier = dust ? 0f : 1.2f;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.Linear(0f, dust ? 0.4f : 1f, 1f, dust ? 1.5f : 0.1f));
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    private void Update()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Shot shot = active[i];
            // Ensure even a very short shot is visible for at least one rendered frame.
            if (shot.SpawnFrame == Time.frameCount)
                continue;

            float previousAge = shot.Age;
            shot.Age += Time.deltaTime;
            if (!shot.Arrived)
            {
                float travelStep = Mathf.Min(Time.deltaTime, Mathf.Max(0f, shot.Duration - previousAge));
                shot.Tracer.Simulate(travelStep, false, false, false);
                if (shot.Age >= shot.Duration)
                {
                    shot.Arrived = true;
                    shot.Tracer.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    if (shot.WallHit)
                        EmitImpact(shot);
                }
            }

            float lifetime = shot.Duration + (shot.WallHit ? 0.65f : 0f);
            if (shot.Age < lifetime)
                continue;

            shot.Tracer.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            shot.Sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            shot.Dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            shot.Root.SetActive(false);
            active.RemoveAt(i);
            available.Push(shot);
        }
    }

    private static void EmitImpact(Shot shot)
    {
        Vector3 normal = shot.Normal.normalized;
        Vector3 point = shot.End + normal * 0.015f;
        shot.Sparks.Play(false);
        shot.Dust.Play(false);
        for (int i = 0; i < 14; i++)
        {
            Vector3 direction = (normal + Random.insideUnitSphere * 0.7f).normalized;
            shot.Sparks.Emit(new ParticleSystem.EmitParams
            {
                position = point,
                velocity = direction * Random.Range(1f, 3.5f),
                startLifetime = Random.Range(0.12f, 0.3f),
                startSize = Random.Range(0.02f, 0.045f),
                startColor = new Color(1f, 0.65f, 0.16f, 1f)
            }, 1);
        }
        for (int i = 0; i < 7; i++)
        {
            shot.Dust.Emit(new ParticleSystem.EmitParams
            {
                position = point,
                velocity = normal * Random.Range(0.2f, 0.65f) + Random.insideUnitSphere * 0.15f,
                startLifetime = Random.Range(0.3f, 0.55f),
                startSize = Random.Range(0.1f, 0.2f),
                startColor = new Color(0.55f, 0.5f, 0.43f, 0.35f)
            }, 1);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
