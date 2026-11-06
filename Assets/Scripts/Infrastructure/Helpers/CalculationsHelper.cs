using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CalculationHelper
{
    public static bool TryGetMouseAimPoint(Controller owner, Weapon weapon, Transform cam,
        LayerMask aimMask, out Vector3 aimPoint)
    {
        aimPoint = default;
        Camera aimCamera = cam != null ? cam.GetComponent<Camera>() : null;
        if (aimCamera == null) aimCamera = Camera.main;
        if (Mouse.current == null || aimCamera == null)
            return false;

        Vector2 cursor = Mouse.current.position.ReadValue();
        if (!aimCamera.pixelRect.Contains(cursor))
            return false;

        Ray cursorRay = aimCamera.ScreenPointToRay(cursor);
        float closestDistance = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(cursorRay, aimCamera.farClipPlane,
                     aimMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(owner.transform) ||
                hit.collider.transform.IsChildOf(weapon.transform) ||
                hit.collider.GetComponentInParent<Controller>() == owner)
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                aimPoint = hit.point;
            }
        }

        if (!float.IsPositiveInfinity(closestDistance))
            return true;

        // Keep aiming into empty space at muzzle height when no surface is under the cursor.
        Plane aimPlane = new Plane(Vector3.up, weapon.transform.position);
        if (!aimPlane.Raycast(cursorRay, out float distance))
            return false;

        aimPoint = cursorRay.GetPoint(distance);
        return true;
    }

    private static int groundMask = 8;
    private static int holdMask = 64;
    private static int balanceBeamMask = 128;
    private static GameObject debugSelectedEdge;


    /// <summary>
    /// Method <c>GetClosestEdgeObject</c> Returns closest hold object in point direction.
    /// </summary>
    public static GameObject GetClosestEdgeObject(Vector3 colliderCenterPoint, Vector3 closestPoint,
        Transform playerTransform, float radius, GameObject currentHold, float maximumEdgeSlope = 20f)
    {
        GameObject closestEdgeObject = null;
        float closestDistance = Mathf.Infinity;

        Collider[] edges = Physics.OverlapSphere(colliderCenterPoint, radius, holdMask);

        foreach (Collider edge in edges)
        {
            if (!CanGrabHold(edge.gameObject, closestPoint, playerTransform, currentHold, maximumEdgeSlope)) continue;

            float distance = Vector3.Distance(edge.transform.position, closestPoint);
            if (closestDistance > distance)
            {
                closestDistance = distance;
                closestEdgeObject = edge.gameObject;
            }
        }

        SetClosestEdgeDebugHighlight(closestEdgeObject);
        return closestEdgeObject;
    }

    // Shared eligibility rules for grabbing and the nearby-hold preview.
    // Range is checked by the caller's overlap sphere.
    public static bool CanGrabHold(GameObject hold, Vector3 closestPoint,
        Transform playerTransform, GameObject currentHold, float maximumEdgeSlope = 20f)
    {
        if (!hold || hold == currentHold) return false;

        float verticalAmount = Mathf.Abs(Vector3.Dot(hold.transform.right.normalized, Vector3.up));
        float maximumVerticalAmount = Mathf.Sin(Mathf.Clamp(maximumEdgeSlope, 0f, 90f) * Mathf.Deg2Rad);
        if (verticalAmount > maximumVerticalAmount || !IsTopFacingHold(hold.transform)) return false;

        Vector3 pointerDirection = closestPoint - playerTransform.position;
        if (Vector3.Angle(pointerDirection, hold.transform.forward) > 80f) return false;
        return Vector3.Dot(playerTransform.forward.normalized,
            (hold.transform.position - playerTransform.position).normalized) >= 0f;
    }

    // EdgeSpawner.forward is the negative sum of the adjacent outward face
    // normals. A top ledge therefore points down; an underside points up.
    // Local up is deliberately made skyward by the spawner and cannot classify this.
    public static bool IsTopFacingHold(Transform hold)
    {
        return Vector3.Dot(-hold.forward, Vector3.up) >= .08715574f; // 5 degrees above horizontal
    }

    static void SetClosestEdgeDebugHighlight(GameObject hold)
    {
        if (debugSelectedEdge != hold && debugSelectedEdge && debugSelectedEdge.TryGetComponent(out HoldSelectionDebug previous))
            previous.SetSelected(false);

        debugSelectedEdge = hold;
        if (!debugSelectedEdge) return;

        HoldSelectionDebug highlight = debugSelectedEdge.GetComponent<HoldSelectionDebug>();
        if (!highlight) highlight = debugSelectedEdge.AddComponent<HoldSelectionDebug>();
        highlight.SetSelected(true);
    }

    /// <summary>
    /// Method <c>GetSurfaceNormalFromHold</c> Returns surface normal of the adjacent hold object.
    /// </summary>
    public static Vector3 GetSurfaceNormalFromHold(Transform transform)
    {
        Vector3 holdPosition = transform.position;
        Vector3 holdNormal = transform.forward;

        Ray directionRay = new Ray(holdPosition - holdNormal + Vector3.down * 0.05f, holdNormal);

        if (Physics.Raycast(directionRay, out RaycastHit surfaceHit, 2, groundMask))
        {
            return surfaceHit.normal;
        }
        return Vector3.zero;
    }

    public static Vector3 GetHoldUpVector(Transform holdTransform)
    {
        float dotProduct = Vector3.Dot(holdTransform.up, Vector3.up);
        return dotProduct > 0 ? holdTransform.up : -holdTransform.up;
    }

    public static Vector3 GetSurfaceForwardFromHold(Transform transform)
    {
        Vector3 holdUpVector = GetHoldUpVector(transform);
        return -Vector3.Lerp(transform.forward, holdUpVector, 0.5f);
    }

    public static Vector3 GetSurfaceUpFromHold(Transform transform)
    {
        Vector3 holdUpVector = GetHoldUpVector(transform);
        return Vector3.Lerp(transform.forward, -holdUpVector, 0.5f);
    }

    public static GameObject GetClosestHoldForClimbing(Vector3 sphereCenterPoint, float radius, GameObject currentHold)
    {
        GameObject closestObject = null;
        float closestDistance = Mathf.Infinity;

        Collider[] holds = Physics.OverlapSphere(sphereCenterPoint, radius, holdMask);
        if (holds.Length == 0)
        {
            SetClosestEdgeDebugHighlight(null);
            return null;
        }

        foreach (Collider hold in holds)
        {
            float holdAngle = Vector3.Angle(Vector3.up, hold.transform.forward);

            if (hold.gameObject == currentHold) continue;
            if (!IsTopFacingHold(hold.transform)) continue;
            if (holdAngle < 95 || holdAngle > 165) continue;

            Vector3 movePos = hold.transform.position + Vector3.down * 0.75f + GetSurfaceNormalFromHold(hold.transform) * 0.26f;
            if (Physics.CheckCapsule(movePos + Vector3.up * 0.25f, movePos - Vector3.up * 0.25f, 0.25f, groundMask)) continue;

            float distance = Vector3.Distance(hold.transform.position, sphereCenterPoint);
            if (closestDistance > distance)
            {
                closestDistance = distance;
                closestObject = hold.gameObject;
            }
        }
        SetClosestEdgeDebugHighlight(closestObject);
        return closestObject;
    }

    public static float randomFloatBetween(float min, float max)
    {
        System.Random random = new System.Random();

        double val = random.NextDouble();

        return (float)(val * (max - min) + min);
    }

}
