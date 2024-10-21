using RoR2;
using UnityEngine;

namespace EntityStates.Toolbot;

public class DroneProjectileHoverStun : DroneProjectileHover
{
	protected override void Pulse()
	{
		BlastAttack blastAttack = new BlastAttack
		{
			baseDamage = 0f,
			baseForce = 0f,
			attacker = (base.projectileController ? base.projectileController.owner : null),
			inflictor = base.gameObject,
			bonusForce = Vector3.zero,
			attackerFiltering = AttackerFiltering.NeverHitSelf,
			crit = false,
			damageColorIndex = DamageColorIndex.Default,
			damageType = DamageType.Stun1s,
			falloffModel = BlastAttack.FalloffModel.None,
			procChainMask = default(ProcChainMask),
			position = base.transform.position,
			procCoefficient = 0f,
			teamIndex = (base.projectileController ? base.projectileController.teamFilter.teamIndex : TeamIndex.None),
			radius = pulseRadius
		};
		blastAttack.Fire();
		EffectData effectData = new EffectData();
		effectData.origin = blastAttack.position;
		effectData.scale = blastAttack.radius;
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/ExplosionVFX"), effectData, transmit: true);
	}
}
