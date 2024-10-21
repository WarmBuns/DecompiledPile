using RoR2;

namespace EntityStates.Toolbot;

public class NailgunSpinDown : BaseNailgunState
{
	public static string spinDownSound;

	public static float baseDuration;

	protected override float GetBaseDuration()
	{
		return baseDuration;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlayAttackSpeedSound(spinDownSound, base.gameObject, attackSpeedStat);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new NailgunFinalBurst
			{
				activatorSkillSlot = base.activatorSkillSlot
			});
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
