using UnityEngine;

public sealed class GeometryDebugModule
{
    public void Draw()
    {
        if (EdgeSolver.balanceBeamsByObjects == null) return;
        foreach (var entry in EdgeSolver.balanceBeamsByObjects)
        {
            if (entry.Key == null) continue;
            foreach (BalanceBeam beam in entry.Value)
                Debug.DrawLine(entry.Key.transform.TransformPoint(beam.point1),
                    entry.Key.transform.TransformPoint(beam.point2), Color.blue);
        }
    }
}
