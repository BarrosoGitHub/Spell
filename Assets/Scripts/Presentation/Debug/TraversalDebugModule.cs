using System.Collections.Generic;
using UnityEngine;

public sealed class TraversalDebugModule
{
    private readonly HashSet<HoldSelectionDebug> highlighted = new HashSet<HoldSelectionDebug>();
    private readonly HashSet<HoldSelectionDebug> reachable = new HashSet<HoldSelectionDebug>();
    private HoldSelectionDebug selected;
    private Collider[] overlapBuffer = new Collider[32];

    public void Refresh(PlayerController player, bool highlights, bool rays)
    {
        if (player == null || !player.isActiveAndEnabled)
        {
            Clear();
            return;
        }
        if (rays)
        {
            Debug.DrawRay(player.transform.position, player.transform.forward, Color.red);
            if (player.closestHold != null)
                Debug.DrawRay(player.closestHold.transform.position, player.closestHold.transform.forward * 0.25f, Color.blue);
        }
        if (!highlights)
        {
            Clear();
            return;
        }
        HoldSelectionDebug nextSelected = GetHighlight(player.closestHold);
        if (selected != nextSelected && selected != null) selected.SetSelected(false);
        selected = nextSelected;
        if (selected != null) selected.SetSelected(true);

        reachable.Clear();
        if (player.State == player.movingState && !player.isGrounded)
        {
            Vector3 center = player.transform.position + Vector3.up * 0.75f;
            Vector3 target = player.transform.position + player.GetRelativeDirection();
            int count = Physics.OverlapSphereNonAlloc(center, 0.6f, overlapBuffer, 64);
            while (count == overlapBuffer.Length && overlapBuffer.Length < 512)
            {
                System.Array.Resize(ref overlapBuffer, overlapBuffer.Length * 2);
                count = Physics.OverlapSphereNonAlloc(center, 0.6f, overlapBuffer, 64);
            }
            // A full bounded buffer falls back to an allocating query rather than hiding holds.
            Collider[] results = count == overlapBuffer.Length ? Physics.OverlapSphere(center, 0.6f, 64) : overlapBuffer;
            if (results != overlapBuffer) count = results.Length;
            for (int i = 0; i < count; i++)
            {
                Collider hold = results[i];
                if (!CalculationHelper.CanGrabHold(hold.gameObject, target, player.transform, player.closestHold)) continue;
                HoldSelectionDebug highlight = GetHighlight(hold.gameObject);
                reachable.Add(highlight);
                highlight.SetGrabbable(true);
            }
            System.Array.Clear(overlapBuffer, 0, overlapBuffer.Length);
        }
        foreach (HoldSelectionDebug highlight in highlighted)
            if (highlight != null && !reachable.Contains(highlight)) highlight.SetGrabbable(false);
        highlighted.Clear();
        foreach (HoldSelectionDebug highlight in reachable) highlighted.Add(highlight);
    }

    private static HoldSelectionDebug GetHighlight(GameObject hold)
    {
        if (hold == null) return null;
        HoldSelectionDebug highlight = hold.GetComponent<HoldSelectionDebug>();
        return highlight != null ? highlight : hold.AddComponent<HoldSelectionDebug>();
    }

    public void Clear()
    {
        if (selected != null) selected.SetSelected(false);
        selected = null;
        foreach (HoldSelectionDebug highlight in highlighted)
            if (highlight != null) highlight.SetGrabbable(false);
        highlighted.Clear();
        reachable.Clear();
    }
}
