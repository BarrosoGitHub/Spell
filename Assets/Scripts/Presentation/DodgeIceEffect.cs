using UnityEngine;
using UnityEngine.VFX;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class DodgeIceEffect : MonoBehaviour
{
    [SerializeField] private VisualEffectAsset effectAsset;
    [SerializeField, Min(0.1f)] private float patchSpacing = 0.3f;
    private const int PatchCount = 48;
    private readonly VisualEffect[] patches = new VisualEffect[PatchCount];
    private PlayerController controller;
    private GameObject trailRoot;
    private Vector3 lastPosition;
    private int nextPatch;
    private bool dodging;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        if (effectAsset == null)
        {
            Debug.LogError("Assign the DodgeIce VFX Graph to DodgeIceEffect.", this);
            enabled = false;
            return;
        }
        // Independent transforms keep deposited ice stationary as the player moves.
        trailRoot = new GameObject("Dodge Frozen Trail");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(trailRoot, gameObject.scene);
        for (int i = 0; i < patches.Length; i++)
        {
            var patch = new GameObject("Ice Patch");
            patch.transform.SetParent(trailRoot.transform, false);
            var effect = patch.AddComponent<VisualEffect>();
            effect.initialEventName = "OnStop";
            effect.visualEffectAsset = effectAsset;
            effect.Stop();
            patches[i] = effect;
        }
    }

    private void OnEnable()
    {
        controller.OnExecutingActionInstance += OnExecuting;
        controller.OnCompleteActionInstance += OnFinished;
        controller.OnCancelActionInstance += OnFinished;
    }

    private void OnExecuting(float duration, ActionInstance action)
    {
        if (action is not Dodge) return;
        dodging = true;
        lastPosition = transform.position;
        PlacePatch(lastPosition);
    }

    private void LateUpdate()
    {
        if (!dodging) return;
        DepositTrail();
    }

    private void DepositTrail()
    {
        Vector3 position = transform.position;
        // Bounded interpolation fills gaps even when a frame spans most of the dodge.
        for (int i = 0; i < PatchCount && Vector3.Distance(lastPosition, position) >= patchSpacing; i++)
        {
            lastPosition = Vector3.MoveTowards(lastPosition, position, patchSpacing);
            PlacePatch(lastPosition);
        }
    }

    private void PlacePatch(Vector3 position)
    {
        if (!Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit,
                3f, controller.groundMask, QueryTriggerInteraction.Ignore)) return;
        VisualEffect patch = patches[nextPatch];
        nextPatch = (nextPatch + 1) % patches.Length;
        patch.transform.SetPositionAndRotation(hit.point + hit.normal * 0.025f,
            Quaternion.FromToRotation(Vector3.forward, hit.normal));
        patch.Reinit();
        patch.Play();
    }

    private void OnFinished(ActionInstance action)
    {
        if (action is not Dodge) return;
        DepositTrail();
        dodging = false;
    }

    private void OnDisable()
    {
        controller.OnExecutingActionInstance -= OnExecuting;
        controller.OnCompleteActionInstance -= OnFinished;
        controller.OnCancelActionInstance -= OnFinished;
        dodging = false;
        foreach (VisualEffect patch in patches)
        {
            if (patch == null) continue;
            patch.Reinit();
            patch.Stop();
        }
    }

    private void OnDestroy()
    {
        if (trailRoot != null) Destroy(trailRoot);
    }
}
