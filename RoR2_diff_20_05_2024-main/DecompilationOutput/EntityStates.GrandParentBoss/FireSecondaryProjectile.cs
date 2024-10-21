using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.GrandParentBoss;

public class FireSecondaryProjectile : BaseState
{
	[SerializeField]
	public float baseDuration = 3f;

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public GameObject muzzleEffectPrefab;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float force;

	[SerializeField]
	public string muzzleName = "SecondaryProjectileMuzzle";

	[SerializeField]
	public string animationStateName = "FireSecondaryProjectile";

	[SerializeField]
	public string playbackRateParam = "FireSecondaryProjectile.playbackRate";

	[SerializeField]
	public string animationLayerName = "Body";

	[SerializeField]
	public float baseFireDelay;

	private float duration;

	private float fireDelay;

	private bool hasFired;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		fireDelay = baseFireDelay / attackSpeedStat;
		if (fireDelay <= Time.fixedDeltaTime * 2f)
		{
			Fire();
		}
		PlayCrossfade(animationLayerName, animationStateName, playbackRateParam, duration, 0.2f);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!hasFired && base.fixedAge >= fireDelay)
		{
			Fire();
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void Fire()
	{
		hasFired = true;
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		if (!base.isAuthority || !projectilePrefab)
		{
			return;
		}
		Ray aimRay = GetAimRay();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				aimRay.origin = component.FindChild(muzzleName).transform.position;
			}
		}
		ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
	}
}
