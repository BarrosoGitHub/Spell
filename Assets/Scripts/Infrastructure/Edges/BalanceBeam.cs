using UnityEngine;

public struct BalanceBeam {
    public Vector3 point1;
    public Vector3 point2;
    public Vector3 perpendicularVector;

    public BalanceBeam(Vector3 point1, Vector3 point2) {
        this.point1 = point1;
        this.point2 = point2;

        Vector3 balanceVector3 = point2 - point1;

        this.perpendicularVector = Vector3.Cross(balanceVector3, Vector3.up);
    }   
}