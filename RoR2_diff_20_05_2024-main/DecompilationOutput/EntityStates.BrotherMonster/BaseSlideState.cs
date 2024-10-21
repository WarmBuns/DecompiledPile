using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BrotherMonster;

public class BaseSlideState : BaseState
{
	public static float duration;

	public static AnimationCurve speedCoefficientCurve;

	public static AnimationCurve jumpforwardSpeedCoefficientCurve;

	public static string soundString;

	public static GameObject slideEffectPrefab;

	public static string slideEffectMuzzlestring;

	protected Vector3 slideVector;

	protected Quaternion slideRotation;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(soundString, base.gameObject);
		if ((bool)base.inputBank)
		{
			_ = (bool)base.characterDirection;
		}
		if (NetworkServer.active)
		{
			Util.CleanseBody(base.characterBody, removeDebuffs: true, removeBuffs: false, removeCooldownBuffs: false, removeDots: false, removeStun: false, removeNearbyProjectiles: false);
		}
		if ((bool)slideEffectPrefab && (bool)base.characterBody)
		{
			Vector3 position = base.characterBody.corePosition;
			Quaternion rotation = Quaternion.identity;
			Transform transform = FindModelChild(slideEffectMuzzlestring);
			if ((bool)transform)
			{
				position = transform.position;
			}
			if ((bool)base.characterDirection)
			{
				rotation = Util.QuaternionSafeLookRotation(slideRotation * base.characterDirection.forward, Vector3.up);
			}
			EffectManager.SimpleEffect(slideEffectPrefab, position, rotation, transmit: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			Vector3 vector = Vector3.zero;
			if ((bool)base.inputBank && (bool)base.characterDirection)
			{
				vector = base.characterDirection.forward;
			}
			if ((bool)base.characterMotor)
			{
				float num = speedCoefficientCurve.Evaluate(base.fixedAge / duration);
				base.characterMotor.rootMotion += slideRotation * (num * moveSpeedStat * vector * GetDeltaTime());
			}
			if (base.fixedAge >= duration)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override void OnExit()
	{
		if (!outer.destroying)
		{
			PlayImpactAnimation();
		}
		base.OnExit();
	}

	private void PlayImpactAnimation()
	{
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
