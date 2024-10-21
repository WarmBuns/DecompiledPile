using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Huntress.HuntressWeapon;

public class FireGlaive : BaseState
{
	public static GameObject projectilePrefab;

	public static float baseDuration = 2f;

	public static float damageCoefficient = 1.2f;

	public static float force = 20f;

	private float duration;

	private static int FireGlaiveStateHash = Animator.StringToHash("FireGlaive");

	private static int FireGlaiveParamHash = Animator.StringToHash("FireGlaive.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Ray aimRay = GetAimRay();
		StartAimMode(aimRay);
		Transform modelTransform = GetModelTransform();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture", FireGlaiveStateHash, FireGlaiveParamHash, duration);
		Vector3 position = aimRay.origin;
		Quaternion rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("RightHand");
				if ((bool)transform)
				{
					position = transform.position;
				}
			}
		}
		if (base.isAuthority)
		{
			ProjectileManager.instance.FireProjectile(projectilePrefab, position, rotation, base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
