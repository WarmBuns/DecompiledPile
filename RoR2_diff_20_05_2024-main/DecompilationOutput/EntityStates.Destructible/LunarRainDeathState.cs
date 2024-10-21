using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Destructible;

public class LunarRainDeathState : BaseState
{
	public static GameObject chargePrefab;

	[Tooltip("How long the object will wait before exploding")]
	public static float chargeDuration;

	public static GameObject explosionEffectPrefab;

	public static float baseDamage;

	public static float explosionRadius;

	public static float explosionDamageCoefficient;

	public static float explosionProcCoefficient;

	public static float explosionForce;

	public static float vfxScale;

	public static bool canRejectForce;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.teamComponent != null)
		{
			base.teamComponent.teamIndex = TeamIndex.Monster;
		}
		if ((bool)chargePrefab)
		{
			EffectData effectData = new EffectData
			{
				scale = explosionRadius,
				origin = base.transform.position
			};
			EffectManager.SpawnEffect(chargePrefab, effectData, transmit: false);
		}
		FlashEmission component = base.modelLocator.modelTransform.GetComponent<FlashEmission>();
		if ((bool)component)
		{
			component.StartFlash();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= chargeDuration && NetworkServer.active)
		{
			Explode();
		}
	}

	public virtual void Explode()
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
		if ((bool)explosionEffectPrefab)
		{
			EffectManager.SpawnEffect(explosionEffectPrefab, new EffectData
			{
				origin = base.transform.position,
				scale = vfxScale,
				rotation = base.transform.rotation
			}, transmit: true);
		}
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.attacker = base.gameObject;
		blastAttack.damageColorIndex = DamageColorIndex.Item;
		blastAttack.baseDamage = baseDamage * explosionDamageCoefficient * Run.instance.teamlessDamageCoefficient;
		blastAttack.radius = explosionRadius;
		blastAttack.falloffModel = BlastAttack.FalloffModel.None;
		blastAttack.procCoefficient = explosionProcCoefficient;
		blastAttack.teamIndex = TeamIndex.Monster;
		blastAttack.damageType = DamageType.LunarRuin;
		blastAttack.position = base.transform.position;
		blastAttack.baseForce = explosionForce;
		blastAttack.canRejectForce = canRejectForce;
		blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
		blastAttack.Fire();
		EntityState.Destroy(base.gameObject);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
