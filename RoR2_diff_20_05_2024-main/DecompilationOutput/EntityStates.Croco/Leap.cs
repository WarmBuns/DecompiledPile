using RoR2;

namespace EntityStates.Croco;

public class Leap : BaseLeap
{
	protected override DamageTypeCombo GetBlastDamageType()
	{
		return (crocoDamageTypeController ? crocoDamageTypeController.GetDamageType() : DamageTypeCombo.Generic) | DamageType.Stun1s;
	}

	protected override void DoImpactAuthority()
	{
		base.DoImpactAuthority();
		DetonateAuthority();
		DropAcidPoolAuthority();
	}
}
