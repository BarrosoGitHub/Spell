using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CinemachineOrbitalFollow))]
public class ArenaCameraInput : MonoBehaviour
{
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField, Min(1f)] private float minimumDistance = 14f;
    [SerializeField, Min(1f)] private float maximumDistance = 24f;
    [SerializeField, Min(0f)] private float zoomPerNotch = 2f;
    [SerializeField, Min(0f)] private float rotationPerPixel = 0.18f;
    [SerializeField, Min(0.01f)] private float zoomSmoothTime = 0.15f;

    private float targetDistance;
    private float zoomVelocity;

    private void Awake()
    {
        if (orbitalFollow == null)
            orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
    }

    private void OnEnable()
    {
        targetDistance = Mathf.Clamp(orbitalFollow.Radius, minimumDistance, maximumDistance);
        zoomVelocity = 0f;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !Application.isFocused || Time.timeScale <= 0f) return;

        // Keep aiming independent of orbiting; right mouse remains available for slow motion.
        if (mouse.middleButton.isPressed)
        {
            float yaw = orbitalFollow.HorizontalAxis.Value + mouse.delta.ReadValue().x * rotationPerPixel;
            orbitalFollow.HorizontalAxis.Value = Mathf.Repeat(yaw + 180f, 360f) - 180f;
        }

        targetDistance = Mathf.Clamp(targetDistance - mouse.scroll.ReadValue().y / 120f * zoomPerNotch,
            minimumDistance, maximumDistance);
        orbitalFollow.Radius = Mathf.SmoothDamp(orbitalFollow.Radius, targetDistance, ref zoomVelocity,
            zoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
    }

    private void OnValidate()
    {
        maximumDistance = Mathf.Max(minimumDistance, maximumDistance);
    }
}
