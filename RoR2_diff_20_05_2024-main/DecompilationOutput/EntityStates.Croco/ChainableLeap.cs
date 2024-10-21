using RoR2;

namespace EntityStates.Croco;

public class ChainableLeap : BaseLeap
{
	public static float refundPerHit;

	protected override DamageTypeCombo GetBlastDamageType()
	{
		return DamageType.Stun1s;
	}

	protected override void DoImpactAuthority()
	{
		base.DoImpactAuthority();
		BlastAttack.Result result = DetonateAuthority();
		base.skillLocator.utility.RunRecharge((float)result.hitCount * refundPerHit);
	}
}
