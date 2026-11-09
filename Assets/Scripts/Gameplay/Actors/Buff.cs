using UnityEngine;

public enum BuffType
{
    Buff,
    Debuff
}

[CreateAssetMenu(fileName = "Buff", menuName = "Gameplay/Buff")]
public class Buff : ScriptableObject
{
    [SerializeField] private BuffType type;

    public BuffType Type => type;
}
