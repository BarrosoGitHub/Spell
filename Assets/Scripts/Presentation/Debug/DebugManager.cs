using UnityEngine;

[DisallowMultipleComponent]
public sealed class DebugManager : MonoBehaviour
{
    [SerializeField] private bool showPursuitIndicators = true;
    [SerializeField] private bool showEnemyLevels = true;
    [SerializeField] private bool showPlayerStats = true;
    [SerializeField] private bool showAttackRange = true;
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private bool showHoldHighlights;
    [SerializeField] private bool showTraversalRays;

    [SerializeField] private bool showGeometry;
    [SerializeField] private bool logCombat;
    [SerializeField] private bool logMissionStates;

    private readonly DamageNumbersDebugModule damageNumbers = new DamageNumbersDebugModule();
    private readonly AttackRangeDebugModule attackRange = new AttackRangeDebugModule();
    private readonly GeometryDebugModule geometry = new GeometryDebugModule();
    private readonly GameplayLoggingDebugModule logging = new GameplayLoggingDebugModule();
    private EntityManager interactions;
    private MissionManager missions;

    private readonly EnemyLabelsDebugModule enemyLabels = new EnemyLabelsDebugModule();
    private readonly PlayerStatsDebugModule playerStats = new PlayerStatsDebugModule();
    private readonly TraversalDebugModule traversal = new TraversalDebugModule();
    private PlayerController player;
    private Camera viewCamera;
    private float nextDiscovery;

    private void OnEnable()
    {
        interactions = EntityManager.Instance;
    }

    private void Update()
    {
        // Debug discovery is scheduled, including while the game is paused.
        if (Time.unscaledTime >= nextDiscovery)
        {
            nextDiscovery = Time.unscaledTime + 1f;
            player = GameManager.Instance != null ? GameManager.Instance.playerController : null;
            viewCamera = Camera.main;
            if (!interactions) interactions = EntityManager.Instance;
            if (missions == null) missions = FindAnyObjectByType<MissionManager>();
            if (showPursuitIndicators || showEnemyLevels) enemyLabels.Refresh();
        }
        if (!showPursuitIndicators && !showEnemyLevels) enemyLabels.Clear();
        damageNumbers.Bind(showDamageNumbers ? interactions : null, viewCamera, transform);
        logging.LogCombat = logCombat;
        logging.LogMissions = logMissionStates;
        logging.Bind(interactions, missions != null ? missions.selectedMission : null);
        if (showGeometry) geometry.Draw();
        if (showPlayerStats) playerStats.Refresh(player);
        traversal.Refresh(player, showHoldHighlights, showTraversalRays);
        attackRange.Refresh(player, showAttackRange);
    }

    private void OnGUI()
    {
        if (showPlayerStats) playerStats.Draw();
        if (showPursuitIndicators || showEnemyLevels)
            enemyLabels.Draw(viewCamera, showPursuitIndicators, showEnemyLevels);
    }

    private void OnDisable()
    {
        damageNumbers.Clear();
        attackRange.Clear();
        logging.Clear();
        traversal.Clear();
        enemyLabels.Clear();
        interactions = null;
        missions = null;
        player = null;
        viewCamera = null;
        nextDiscovery = 0f;
    }
}
