using System.Collections.Generic;
using UnityEngine;

public sealed class TraversalDebugModule
{
    private readonly HashSet<HoldSelectionDebug> highlighted = new HashSet<HoldSelectionDebug>();
    private readonly HashSet<HoldSelectionDebug> reachable = new HashSet<HoldSelectionDebug>();
    private HoldSelectionDebug selected;

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
            foreach (Collider hold in Physics.OverlapSphere(center, 0.6f, 64))
            {
                if (!CalculationHelper.CanGrabHold(hold.gameObject, target, player.transform, player.closestHold)) continue;
                HoldSelectionDebug highlight = GetHighlight(hold.gameObject);
                reachable.Add(highlight);
                highlight.SetGrabbable(true);
            }
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
