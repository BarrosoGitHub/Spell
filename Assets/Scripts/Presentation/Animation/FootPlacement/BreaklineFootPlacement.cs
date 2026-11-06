using UnityEngine;

namespace Breakline.Animation
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(FootPlacementIK))]
    public sealed class BreaklineFootPlacement : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private bool useFootPlacement = true;
        private FootPlacementIK feet;
        private void Awake() => feet = GetComponent<FootPlacementIK>();
        private void Reset()
        {
            var animation = GetComponent<PlayerAnimationController>();
            if (animation) player = animation.playerController;
        }
        private void Update()
        {
            bool active = useFootPlacement && player && player.isActiveAndEnabled && player.movingState != null &&
                player.State == player.movingState && player.IsGrounded;
            feet.SetGroundedMovement(active, player ? player.groundMask : 0);
        }
        private void OnDisable() { if (feet) feet.SetGroundedMovement(false, 0); }
    }
}
