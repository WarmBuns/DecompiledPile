using UnityEngine;

namespace RoR2.Orbs;

public class SimpleLightningStrikeOrb : GenericDamageOrb, IOrbFixedUpdateBehavior
{
	private Vector3 lastKnownTargetPosition;

	public override void Begin()
	{
		base.Begin();
		base.duration = 0.25f;
		if ((bool)target)
		{
			lastKnownTargetPosition = target.transform.position;
		}
	}

	protected override GameObject GetOrbEffect()
	{
		return OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/SimpleLightningStrikeOrbEffect");
	}

	public override void OnArrival()
	{
		EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/ImpactEffects/SimpleLightningStrikeImpact"), new EffectData
		{
			origin = lastKnownTargetPosition
		}, transmit: true);
		if ((bool)attacker)
		{
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.attacker = attacker;
			blastAttack.baseDamage = damageValue;
			blastAttack.baseForce = 0f;
			blastAttack.bonusForce = Vector3.down * 1500f;
			blastAttack.crit = isCrit;
			blastAttack.damageColorIndex = DamageColorIndex.Item;
			blastAttack.falloffModel = BlastAttack.FalloffModel.None;
			blastAttack.inflictor = null;
			blastAttack.position = lastKnownTargetPosition;
			blastAttack.procChainMask = procChainMask;
			blastAttack.procCoefficient = procCoefficient;
			blastAttack.radius = 3f;
			blastAttack.teamIndex = TeamComponent.GetObjectTeam(attacker);
			blastAttack.Fire();
		}
	}

	public void FixedUpdate()
	{
		if ((bool)target)
		{
			lastKnownTargetPosition = target.transform.position;
		}
	}
}
