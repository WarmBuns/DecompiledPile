using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Destructible;

public class AltarSkeletonDeath : BaseState
{
	public static GameObject explosionEffectPrefab;

	public static float explosionRadius;

	public static string deathSoundString;

	private float stopwatch;

	public static event Action onDeath;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(deathSoundString, base.gameObject);
		Explode();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
	}

	private void Explode()
	{
		if ((bool)base.modelLocator)
		{
			if ((bool)base.modelLocator.modelBaseTransform)
			{
				EntityState.Destroy(base.modelLocator.modelBaseTransform.gameObject);
			}
			if ((bool)base.modelLocator.modelTransform)
			{
				EntityState.Destroy(base.modelLocator.modelTransform.gameObject);
			}
		}
		if ((bool)explosionEffectPrefab && NetworkServer.active)
		{
			EffectManager.SpawnEffect(explosionEffectPrefab, new EffectData
			{
				origin = base.transform.position,
				scale = explosionRadius,
				rotation = Quaternion.identity
			}, transmit: false);
		}
		AltarSkeletonDeath.onDeath?.Invoke();
		EntityState.Destroy(base.gameObject);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
