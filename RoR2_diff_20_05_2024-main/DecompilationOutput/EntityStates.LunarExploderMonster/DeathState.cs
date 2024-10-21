using System.Collections;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.LunarExploderMonster;

public class DeathState : GenericCharacterDeath
{
	public static GameObject deathPreExplosionVFX;

	public static GameObject deathExplosionEffect;

	public static GameObject projectilePrefab;

	public static float projectileVerticalSpawnOffset;

	public static float projectileDamageCoefficient;

	public static int projectileCount;

	public static float duration;

	public static float deathExplosionRadius;

	public static string muzzleName;

	private GameObject deathPreExplosionInstance;

	private Transform muzzleTransform;

	private bool hasExploded;

	private Vector3? explosionPosition;

	protected override bool shouldAutoDestroy => false;

	protected override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
	{
		PlayCrossfade("Body", "Death", "Death.playbackRate", duration, 0.1f);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		muzzleTransform = FindModelChild(muzzleName);
		if (!muzzleTransform)
		{
			return;
		}
		explosionPosition = muzzleTransform.position;
		if ((bool)deathPreExplosionVFX)
		{
			deathPreExplosionInstance = Object.Instantiate(deathPreExplosionVFX, muzzleTransform.position, muzzleTransform.rotation);
			deathPreExplosionInstance.transform.parent = muzzleTransform;
			deathPreExplosionInstance.transform.localScale = Vector3.one * deathExplosionRadius;
			ScaleParticleSystemDuration component = deathPreExplosionInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = duration;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)muzzleTransform)
		{
			explosionPosition = muzzleTransform.position;
		}
		if (base.fixedAge >= duration && !hasExploded)
		{
			if (base.isAuthority)
			{
				FireExplosion();
			}
			base.modelLocator?.modelTransform?.gameObject.SetActive(value: false);
			ProjectileDotZone projectileDotZone = projectilePrefab?.GetComponent<ProjectileDotZone>();
			if ((bool)projectileDotZone)
			{
				base.modelLocator?.StartCoroutine(DoDestroyAfrerProjectileDestroyed(projectileDotZone.lifetime + 0.5f));
			}
			hasExploded = true;
		}
	}

	private IEnumerator DoDestroyAfrerProjectileDestroyed(float delay)
	{
		yield return new WaitForSeconds(delay);
		DestroyModel();
		if (NetworkServer.active)
		{
			DestroyBodyAsapServer();
		}
	}

	public override void OnExit()
	{
		DestroyModel();
		base.OnExit();
	}

	private void FireExplosion()
	{
		if (!hasExploded && (bool)base.cachedModelTransform && base.isAuthority)
		{
			for (int i = 0; i < projectileCount; i++)
			{
				float num = 360f / (float)projectileCount;
				Vector3 forward = Util.QuaternionSafeLookRotation(base.cachedModelTransform.forward, base.cachedModelTransform.up) * Util.ApplySpread(Vector3.forward, 0f, 0f, 1f, 1f, num * (float)i);
				FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
				fireProjectileInfo.projectilePrefab = projectilePrefab;
				fireProjectileInfo.position = base.characterBody.corePosition + Vector3.up * projectileVerticalSpawnOffset;
				fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(forward);
				fireProjectileInfo.owner = base.gameObject;
				fireProjectileInfo.damage = damageStat * projectileDamageCoefficient;
				fireProjectileInfo.crit = Util.CheckRoll(critStat, base.characterBody.master);
				ProjectileManager.instance.FireProjectile(fireProjectileInfo);
			}
			if ((bool)deathExplosionEffect)
			{
				EffectManager.SpawnEffect(deathExplosionEffect, new EffectData
				{
					origin = base.characterBody.corePosition,
					scale = deathExplosionRadius
				}, transmit: true);
			}
		}
	}
}
