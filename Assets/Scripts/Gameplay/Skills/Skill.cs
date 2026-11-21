using UnityEngine;

public abstract class Skill : ScriptableObject
{
    public int Level { get; private set; } = 1;
    public abstract void Enable(Controller controller);
    public abstract void Disable(Controller controller);
}
