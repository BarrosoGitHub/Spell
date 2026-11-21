using UnityEngine;

public sealed class SlowSkill : Skill
{
    public BuffDefinition slowBuff;
    private const float chance = 0.5f;
    public override void Enable(Controller controller)
    {
        controller.OnHit += ApplySlow;
    }

    public override void Disable(Controller controller)
    {
        controller.OnHit -= ApplySlow;
    }

    private void ApplySlow(Controller target, InteractionContext context)
    {
        if (Random.value < chance)
            target.AddBuff(slowBuff);
    }
}
