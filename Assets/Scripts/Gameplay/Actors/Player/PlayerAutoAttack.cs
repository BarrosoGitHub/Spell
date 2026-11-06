using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerAutoAttack : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera viewCamera;
    [SerializeField, Min(0.01f)] private float targetSearchIntervalSeconds = 0.15f;
    [Tooltip("Include enemy colliders. Visibility checks separately include all solid geometry.")]
    [SerializeField] private LayerMask targetMask = ~0;

    private const int MaxRetainedQuerySize = 512;
    private Collider[] overlapBuffer = new Collider[64];
    private RaycastHit[] raycastBuffer = new RaycastHit[32];
    private CombatHandler combatHandler;
    private EnemyController currentEnemy;
    private Collider currentCollider;
    private float nextSearchTime;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        combatHandler = playerController.GetComponent<CombatHandler>();
        if (viewCamera == null)
            viewCamera = Camera.main;
        if (combatHandler == null || viewCamera == null)
        {
            Debug.LogError("PlayerAutoAttack requires a CombatHandler on the player and a view camera.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!Application.isFocused || Time.timeScale <= 0f)
            return;
        if (GameManager.Instance != null && GameManager.Instance.GameState != GameState.Playing)
            return;
        if (!playerController.isActiveAndEnabled || playerController.entity == null ||
            playerController.entity.CurrentHealth <= 0f ||
            (playerController.State != playerController.movingState && playerController.State != playerController.hangingState))
            return;

        Weapon weapon = combatHandler.EquippedWeapon;
        if (weapon == null) return;

        if (Time.time >= nextSearchTime)
        {
            FindTarget(weapon);
            nextSearchTime = Time.time + Mathf.Max(0.01f, targetSearchIntervalSeconds);
        }

        // Recheck visibility before every shot request, including between acquisition scans.
        if (CanSeeEnemy(currentEnemy, currentCollider, weapon))
        {
            var (target, context) = combatHandler.TryExecuteAttack(playerController, currentCollider.bounds.center);
            if (target != null && context != null)
                playerController.AttackController(target, context);
        }
    }

    private void FindTarget(Weapon weapon)
    {
        currentEnemy = null;
        currentCollider = null;
        float closestDistanceSquared = float.PositiveInfinity;
        int count;
        Collider[] candidates;
        while (true)
        {
            count = Physics.OverlapSphereNonAlloc(weapon.transform.position, weapon.Stats.range,
                overlapBuffer, targetMask, QueryTriggerInteraction.Ignore);
            if (count < overlapBuffer.Length)
            {
                candidates = overlapBuffer;
                break;
            }
            if (overlapBuffer.Length >= MaxRetainedQuerySize)
            {
                // Rare overflow must still consider every enemy instead of silently dropping targets.
                candidates = Physics.OverlapSphere(weapon.transform.position, weapon.Stats.range,
                    targetMask, QueryTriggerInteraction.Ignore);
                count = candidates.Length;
                break;
            }
            System.Array.Resize(ref overlapBuffer, overlapBuffer.Length * 2);
        }

        for (int i = 0; i < count; i++)
        {
            Collider candidate = candidates[i];
            float distanceSquared = (candidate.bounds.center - weapon.transform.position).sqrMagnitude;
            if (distanceSquared >= closestDistanceSquared) continue;

            EnemyController enemy = candidate.GetComponentInParent<EnemyController>();
            if (!CanSeeEnemy(enemy, candidate, weapon)) continue;

            closestDistanceSquared = distanceSquared;
            currentEnemy = enemy;
            currentCollider = candidate;
        }
        // Do not retain references to despawned candidates between scans.
        System.Array.Clear(overlapBuffer, 0, overlapBuffer.Length);
    }

    private bool CanSeeEnemy(EnemyController enemy, Collider targetCollider, Weapon weapon)
    {
        if (enemy == null || !enemy.isActiveAndEnabled || enemy.entity == null ||
            enemy.entity.CurrentHealth <= 0f || targetCollider == null ||
            !targetCollider.enabled || !targetCollider.gameObject.activeInHierarchy)
            return false;

        Vector3 point = targetCollider.bounds.center;
        if ((point - weapon.transform.position).sqrMagnitude > weapon.Stats.range * weapon.Stats.range)
            return false;

        Vector3 viewport = viewCamera.WorldToViewportPoint(point);
        if (viewport.z < viewCamera.nearClipPlane || viewport.z > viewCamera.farClipPlane ||
            viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            return false;

        return HasLineOfSight(viewCamera.transform.position, point, enemy, weapon) &&
               HasLineOfSight(weapon.transform.position, point, enemy, weapon);
    }

    private bool HasLineOfSight(Vector3 origin, Vector3 point, EnemyController enemy, Weapon weapon)
    {
        Vector3 direction = point - origin;
        float distance = direction.magnitude;
        if (distance <= Mathf.Epsilon) return false;

        RaycastHit[] hits;
        int count;
        while (true)
        {
            count = Physics.RaycastNonAlloc(origin, direction / distance, raycastBuffer,
                distance + 0.01f, ~0, QueryTriggerInteraction.Ignore);
            if (count < raycastBuffer.Length)
            {
                hits = raycastBuffer;
                break;
            }
            if (raycastBuffer.Length >= MaxRetainedQuerySize)
            {
                hits = Physics.RaycastAll(origin, direction / distance, distance + 0.01f,
                    ~0, QueryTriggerInteraction.Ignore);
                count = hits.Length;
                break;
            }
            System.Array.Resize(ref raycastBuffer, raycastBuffer.Length * 2);
        }

        Controller closestController = null;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider collider = hits[i].collider;
            Controller hitController = collider.GetComponentInParent<Controller>();
            if (hitController == playerController || collider.transform.IsChildOf(playerController.transform) ||
                collider.transform.IsChildOf(weapon.transform))
                continue;
            if (hits[i].distance >= closestDistance) continue;

            closestDistance = hits[i].distance;
            closestController = hitController;
        }
        System.Array.Clear(raycastBuffer, 0, raycastBuffer.Length);
        return closestController == enemy;
    }

    private void OnDisable()
    {
        currentEnemy = null;
        currentCollider = null;
        nextSearchTime = 0f;
    }
}
