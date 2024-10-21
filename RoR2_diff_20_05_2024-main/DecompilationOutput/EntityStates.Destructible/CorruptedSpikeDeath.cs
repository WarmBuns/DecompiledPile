using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Destructible;

public class CorruptedSpikeDeath : BaseState
{
	public static GameObject chargePrefab;

	public static float chargeDuration;

	public static GameObject explosionEffectPrefab;

	public static float explosionRadius;

	public static float explosionDamageCoefficient;

	public static float explosionProcCoefficient;

	public static float explosionForce;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)chargePrefab)
		{
			Object.Instantiate(chargePrefab, base.transform);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge >= chargeDuration)
		{
			Explode();
		}
	}

	private void Explode()
	{
		if ((bool)explosionEffectPrefab)
		{
			EffectManager.SpawnEffect(explosionEffectPrefab, new EffectData
			{
				origin = base.transform.position,
				scale = explosionRadius,
				rotation = Quaternion.identity
			}, transmit: true);
		}
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.attacker = base.gameObject;
		blastAttack.damageColorIndex = DamageColorIndex.Item;
		blastAttack.baseDamage = 1f;
		blastAttack.radius = explosionRadius;
		blastAttack.falloffModel = BlastAttack.FalloffModel.None;
		blastAttack.procCoefficient = explosionProcCoefficient;
		blastAttack.teamIndex = TeamIndex.None;
		blastAttack.position = base.transform.position;
		blastAttack.baseForce = explosionForce;
		blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
		blastAttack.Fire();
		EntityState.Destroy(base.gameObject);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
