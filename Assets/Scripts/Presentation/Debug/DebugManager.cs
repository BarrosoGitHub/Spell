using UnityEngine;

[DisallowMultipleComponent]
public sealed class DebugManager : MonoBehaviour
{
    [SerializeField] private bool showPursuitIndicators = true;
    [SerializeField] private bool showEnemyLevels = true;
    [SerializeField] private bool showPlayerStats = true;
    [SerializeField] private bool showHoldHighlights;
    [SerializeField] private bool showTraversalRays;

    private readonly EnemyLabelsDebugModule enemyLabels = new EnemyLabelsDebugModule();
    private readonly PlayerStatsDebugDisplay playerStats = new PlayerStatsDebugDisplay();
    private readonly TraversalDebugModule traversal = new TraversalDebugModule();
    private PlayerController player;
    private Camera viewCamera;
    private float nextDiscovery;

    private void Update()
    {
        // Debug discovery is scheduled, including while the game is paused.
        if (Time.unscaledTime >= nextDiscovery)
        {
            nextDiscovery = Time.unscaledTime + 1f;
            player = GameManager.Instance != null ? GameManager.Instance.playerController : null;
            viewCamera = Camera.main;
            if (showPursuitIndicators || showEnemyLevels) enemyLabels.Refresh();
        }
        if (showPlayerStats) playerStats.Refresh(player);
        traversal.Refresh(player, showHoldHighlights, showTraversalRays);
    }

    private void OnGUI()
    {
        if (showPlayerStats) playerStats.Draw();
        if (showPursuitIndicators || showEnemyLevels)
            enemyLabels.Draw(viewCamera, showPursuitIndicators, showEnemyLevels);
    }

    private void OnDisable()
    {
        traversal.Clear();
        enemyLabels.Clear();
        player = null;
        viewCamera = null;
        nextDiscovery = 0f;
    }
}
