using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Railgunner.Weapon;

public class BaseFireMine : BaseState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float baseCrossfadeDuration;

	[SerializeField]
	public GameObject muzzleEffectPrefab;

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float force;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		float crossfadeDuration = baseCrossfadeDuration / attackSpeedStat;
		Util.PlaySound(enterSoundString, base.gameObject);
		Ray aimRay = GetAimRay();
		StartAimMode(aimRay);
		if ((bool)GetModelAnimator())
		{
			PlayCrossfade(animationLayerName, animationStateName, animationPlaybackRateParam, duration, crossfadeDuration);
		}
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		if (base.isAuthority)
		{
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
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
