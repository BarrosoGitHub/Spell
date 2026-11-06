using UnityEngine;

[System.Serializable]
public struct ActionStats
{
    public float preparingTime;
    public float executingTime;
}

[CreateAssetMenu(fileName = "ScriptableObject", menuName = "Actions/Player")]
public class PlayerActionsSO : ScriptableObject
{
    public ActionStats Grab;
    public ActionStats ClimbUp;
    public ActionStats ClimbEject;
    public ActionStats ClimbJump;
    public ActionStats Dodge = new ActionStats { preparingTime = 0f, executingTime = 0.2f };
}
