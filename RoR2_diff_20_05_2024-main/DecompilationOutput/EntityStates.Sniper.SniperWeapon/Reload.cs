using RoR2;
using UnityEngine;

namespace EntityStates.Sniper.SniperWeapon;

public class Reload : BaseState
{
	public static float baseDuration = 1f;

	public static float reloadTimeFraction = 0.75f;

	public static string soundString = "";

	private float duration;

	private float reloadTime;

	private Animator modelAnimator;

	private bool reloaded;

	private EntityStateMachine scopeStateMachine;

	private static int PrepBarrageStateHash = Animator.StringToHash("PrepBarrage");

	private static int PrepBarrageParamHash = Animator.StringToHash("PrepBarrage.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		reloadTime = duration * reloadTimeFraction;
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayAnimation("Gesture", PrepBarrageStateHash, PrepBarrageParamHash, duration);
		}
		if ((bool)base.skillLocator)
		{
			GenericSkill secondary = base.skillLocator.secondary;
			if ((bool)secondary)
			{
				scopeStateMachine = secondary.stateMachine;
			}
		}
		if (base.isAuthority && (bool)scopeStateMachine)
		{
			scopeStateMachine.SetNextState(new LockSkill());
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!reloaded && base.fixedAge >= reloadTime)
		{
			if ((bool)base.skillLocator)
			{
				GenericSkill primary = base.skillLocator.primary;
				if ((bool)primary)
				{
					primary.Reset();
					Util.PlaySound(soundString, base.gameObject);
				}
			}
			reloaded = true;
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if (base.isAuthority && (bool)scopeStateMachine)
		{
			scopeStateMachine.SetNextStateToMain();
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
