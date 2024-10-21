using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Destructible;

public class GauntletShardDeath : BaseState
{
	public static GameObject explosionEffectPrefab;

	public static float explosionRadius;

	public static float explosionDamageCoefficient;

	public static float explosionProcCoefficient;

	public static float explosionForce;

	private float stopwatch;

	public override void OnEnter()
	{
		base.OnEnter();
		Explode();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
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
			}, transmit: true);
		}
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.attacker = base.gameObject;
		blastAttack.damageColorIndex = DamageColorIndex.Item;
		blastAttack.baseDamage = damageStat * explosionDamageCoefficient * Run.instance.teamlessDamageCoefficient;
		blastAttack.radius = explosionRadius;
		blastAttack.falloffModel = BlastAttack.FalloffModel.None;
		blastAttack.procCoefficient = explosionProcCoefficient;
		blastAttack.teamIndex = TeamIndex.None;
		blastAttack.position = base.transform.position;
		blastAttack.baseForce = explosionForce;
		blastAttack.bonusForce = explosionForce * 0.5f * Vector3.up;
		blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
		blastAttack.Fire();
		EntityState.Destroy(base.gameObject);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
