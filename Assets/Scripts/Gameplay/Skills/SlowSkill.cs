using UnityEngine;

public sealed class SlowSkill : Skill
{
    public override public void Activate()
    {
        // This skill is passive and does not have an active effect.
    }

    private void ApplySlow(Controller target, InteractionContext context)
    {
        if (target != null && slowBuff != null && Random.value < chance)
            target.AddBuff(slowBuff);
    }
}
