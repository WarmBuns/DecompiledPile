using RoR2;
using UnityEngine;

namespace EntityStates.Assassin2;

public class ChargeDash : BaseState
{
	public static float baseDuration = 1.5f;

	public static string enterSoundString;

	private Animator modelAnimator;

	private float duration;

	private int slashCount;

	private Transform modelTransform;

	private Vector3 oldVelocity;

	private bool dashComplete;

	private static int PreAttackStateHash = Animator.StringToHash("PreAttack");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(enterSoundString, base.gameObject);
		modelTransform = GetModelTransform();
		AimAnimator component = modelTransform.GetComponent<AimAnimator>();
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		if ((bool)component)
		{
			component.enabled = true;
		}
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = GetAimRay().direction;
		}
		if ((bool)modelAnimator)
		{
			PlayAnimation("Gesture", PreAttackStateHash);
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration && base.isAuthority)
		{
			outer.SetNextState(new DashStrike());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
