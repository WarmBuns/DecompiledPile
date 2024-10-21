using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Engi.Mine;

public class Detonate : BaseMineState
{
	public static float blastRadius;

	public static GameObject explosionEffectPrefab;

	protected override bool shouldStick => false;

	protected override bool shouldRevertToWaitForStickOnSurfaceLost => false;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			Explode();
		}
	}

	private void Explode()
	{
		ProjectileDamage component = GetComponent<ProjectileDamage>();
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		if (base.armingStateMachine?.state is BaseMineArmingState baseMineArmingState)
		{
			num = baseMineArmingState.damageScale;
			num2 = baseMineArmingState.forceScale;
			num3 = baseMineArmingState.blastRadiusScale;
		}
		float num4 = blastRadius * num3;
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.procChainMask = base.projectileController.procChainMask;
		blastAttack.procCoefficient = base.projectileController.procCoefficient;
		blastAttack.attacker = base.projectileController.owner;
		blastAttack.inflictor = base.gameObject;
		blastAttack.teamIndex = base.projectileController.teamFilter.teamIndex;
		blastAttack.procCoefficient = base.projectileController.procCoefficient;
		blastAttack.baseDamage = component.damage * num;
		blastAttack.baseForce = component.force * num2;
		blastAttack.falloffModel = BlastAttack.FalloffModel.None;
		blastAttack.crit = component.crit;
		blastAttack.radius = num4;
		blastAttack.position = base.transform.position;
		blastAttack.damageColorIndex = component.damageColorIndex;
		blastAttack.Fire();
		if ((bool)explosionEffectPrefab)
		{
			EffectManager.SpawnEffect(explosionEffectPrefab, new EffectData
			{
				origin = base.transform.position,
				rotation = base.transform.rotation,
				scale = num4
			}, transmit: true);
		}
		EntityState.Destroy(base.gameObject);
	}
}
