using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.AncientWispMonster;

public class FireRHCannon : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject effectPrefab;

	public static float baseDuration = 2f;

	public static float baseDurationBetweenShots = 0.5f;

	public static float damageCoefficient = 1.2f;

	public static float force = 20f;

	public static int bulletCount;

	private float duration;

	private float durationBetweenShots;

	public int bulletCountCurrent = 1;

	private static int fireRHCannonHash = Animator.StringToHash("FireRHCannon");

	private static int fireRHCannonParamHash = Animator.StringToHash("FireRHCannon.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Ray aimRay = GetAimRay();
		string text = "MuzzleRight";
		duration = baseDuration / attackSpeedStat;
		durationBetweenShots = baseDurationBetweenShots / attackSpeedStat;
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, text, transmit: false);
		}
		PlayAnimation("Gesture", fireRHCannonHash, fireRHCannonParamHash, duration);
		if (!base.isAuthority || !base.modelLocator || !base.modelLocator.modelTransform)
		{
			return;
		}
		ChildLocator component = base.modelLocator.modelTransform.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		Transform transform = component.FindChild(text);
		if ((bool)transform)
		{
			Vector3 forward = aimRay.direction;
			if (Physics.Raycast(aimRay, out var hitInfo, (int)LayerIndex.world.mask))
			{
				forward = hitInfo.point - transform.position;
			}
			ProjectileManager.instance.FireProjectile(projectilePrefab, transform.position, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			if (bulletCountCurrent == bulletCount && base.fixedAge >= duration)
			{
				outer.SetNextStateToMain();
			}
			else if (bulletCountCurrent < bulletCount && base.fixedAge >= durationBetweenShots)
			{
				FireRHCannon fireRHCannon = new FireRHCannon();
				fireRHCannon.bulletCountCurrent = bulletCountCurrent + 1;
				outer.SetNextState(fireRHCannon);
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
