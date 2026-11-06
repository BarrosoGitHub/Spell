using UnityEngine;
using UnityEngine.InputSystem;

public class CameraAimOffset : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [Range(2f, 10f)] [SerializeField] private float cameraTargetDivider = 5f;

    void Update()
    {
        if (Mouse.current == null) return;

        // Get mouse screen position
        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        // Create a ray from camera through mouse
        Ray ray = Camera.main.ScreenPointToRay(mouseScreen);

        // Plane at player height
        Plane plane = new Plane(Vector3.up, playerTransform.position);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorld = ray.GetPoint(distance);

            // Blend player + mouse
            Vector3 targetPos = (mouseWorld + (cameraTargetDivider - 1) * playerTransform.position) / cameraTargetDivider;

            // Keep target Y at player height
            targetPos.y = playerTransform.position.y;

            transform.position = targetPos;
        }
    }
}