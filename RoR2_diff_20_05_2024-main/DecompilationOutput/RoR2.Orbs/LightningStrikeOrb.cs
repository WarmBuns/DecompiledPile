using UnityEngine;

namespace RoR2.Orbs;

public class LightningStrikeOrb : GenericDamageOrb, IOrbFixedUpdateBehavior
{
	private Vector3 lastKnownTargetPosition;

	private static readonly float positionLockDuration = 0.3f;

	public override void Begin()
	{
		base.Begin();
		base.duration = 0.5f;
		if ((bool)target)
		{
			lastKnownTargetPosition = target.transform.position;
		}
	}

	protected override GameObject GetOrbEffect()
	{
		return OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/LightningStrikeOrbEffect");
	}

	public override void OnArrival()
	{
		EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/ImpactEffects/LightningStrikeImpact"), new EffectData
		{
			origin = lastKnownTargetPosition
		}, transmit: true);
		if ((bool)attacker)
		{
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.attacker = attacker;
			blastAttack.baseDamage = damageValue;
			blastAttack.baseForce = 0f;
			blastAttack.bonusForce = Vector3.down * 3000f;
			blastAttack.crit = isCrit;
			blastAttack.damageColorIndex = DamageColorIndex.Item;
			blastAttack.damageType = DamageType.Stun1s;
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
		if ((bool)target && base.timeUntilArrival >= positionLockDuration)
		{
			lastKnownTargetPosition = target.transform.position;
		}
	}
}
