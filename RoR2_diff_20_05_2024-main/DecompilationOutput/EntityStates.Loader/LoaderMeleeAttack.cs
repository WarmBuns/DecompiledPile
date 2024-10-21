using RoR2;
using UnityEngine;

namespace EntityStates.Loader;

public class LoaderMeleeAttack : BasicMeleeAttack
{
	public static GameObject overchargeImpactEffectPrefab;

	public static float barrierPercentagePerHit;

	protected override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
	{
		base.AuthorityModifyOverlapAttack(overlapAttack);
		if (HasBuff(JunkContent.Buffs.LoaderOvercharged))
		{
			overlapAttack.damage *= 2f;
			overlapAttack.hitEffectPrefab = overchargeImpactEffectPrefab;
			overlapAttack.damageType |= (DamageTypeCombo)DamageType.Stun1s;
		}
	}

	protected override void OnMeleeHitAuthority()
	{
		base.OnMeleeHitAuthority();
		base.healthComponent.AddBarrierAuthority(barrierPercentagePerHit * base.healthComponent.fullBarrier);
	}
}
