using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.VoidMegaCrab.Weapon;

public class FireCrabCannonBase : BaseState
{
	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public GameObject muzzleflashEffectPrefab;

	[SerializeField]
	public int projectileCount = 3;

	[SerializeField]
	public float spread;

	[SerializeField]
	public float bonusPitch;

	[SerializeField]
	public float totalYawSpread = 5f;

	[SerializeField]
	public float baseDuration = 2f;

	[SerializeField]
	public float baseFireDuration = 0.2f;

	[SerializeField]
	public float damageCoefficient = 1.2f;

	[SerializeField]
	public float force = 20f;

	[SerializeField]
	public string attackSound;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public string animationLayerName = "Gesture, Additive";

	[SerializeField]
	public string animationStateName = "FireCrabCannon";

	[SerializeField]
	public string animationPlaybackRateParam = "FireCrabCannon.playbackRate";

	private float duration;

	private float fireDuration;

	private int projectilesFired;

	private Transform muzzleTransform;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		fireDuration = baseFireDuration / attackSpeedStat;
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		muzzleTransform = FindModelChild(muzzleName);
		if (!muzzleTransform)
		{
			muzzleTransform = base.characterBody.aimOriginTransform;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		int num = Mathf.FloorToInt(base.fixedAge / fireDuration * (float)projectileCount);
		if (projectilesFired <= num && projectilesFired < projectileCount)
		{
			FireProjectile();
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void FireProjectile()
	{
		Util.PlaySound(attackSound, base.gameObject);
		base.characterBody.SetAimTimer(3f);
		if ((bool)muzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		if (base.isAuthority)
		{
			Ray aimRay = GetAimRay();
			int num = Mathf.FloorToInt((float)projectilesFired - (float)(projectileCount - 1) / 2f);
			float bonusYaw = 0f;
			if (projectileCount > 1)
			{
				bonusYaw = (float)num / (float)(projectileCount - 1) * totalYawSpread;
			}
			Vector3 forward = Util.ApplySpread(aimRay.direction, 0f, spread, 1f, 1f, bonusYaw, bonusPitch);
			Vector3 position = muzzleTransform.position;
			ProjectileManager.instance.FireProjectile(projectilePrefab, position, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
		}
		projectilesFired++;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
