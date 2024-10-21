using EntityStates.VagrantMonster;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VagrantNovaItem;

public class DetonateState : BaseVagrantNovaItemState
{
	public static float blastRadius;

	public static float blastDamageCoefficient;

	public static float blastProcCoefficient;

	public static float blastForce;

	public static float baseDuration;

	public static string explosionSound;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		if (NetworkServer.active && (bool)base.attachedBody)
		{
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.attacker = base.attachedBody.gameObject;
			blastAttack.baseDamage = base.attachedBody.damage * blastDamageCoefficient;
			blastAttack.baseForce = blastForce;
			blastAttack.bonusForce = Vector3.zero;
			blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
			blastAttack.crit = base.attachedBody.RollCrit();
			blastAttack.damageColorIndex = DamageColorIndex.Item;
			blastAttack.damageType = DamageType.Generic;
			blastAttack.falloffModel = BlastAttack.FalloffModel.None;
			blastAttack.inflictor = base.gameObject;
			blastAttack.position = base.attachedBody.corePosition;
			blastAttack.procChainMask = default(ProcChainMask);
			blastAttack.procCoefficient = blastProcCoefficient;
			blastAttack.radius = blastRadius;
			blastAttack.losType = BlastAttack.LoSType.NearestHit;
			blastAttack.teamIndex = base.attachedBody.teamComponent.teamIndex;
			blastAttack.Fire();
			EffectData effectData = new EffectData();
			effectData.origin = base.attachedBody.corePosition;
			effectData.SetHurtBoxReference(base.attachedBody.mainHurtBox);
			EffectManager.SpawnEffect(FireMegaNova.novaEffectPrefab, effectData, transmit: true);
		}
		SetChargeSparkEmissionRateMultiplier(0f);
		Util.PlaySound(explosionSound, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextState(new RechargeState());
		}
	}
}
