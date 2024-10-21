namespace EntityStates.Loader;

public class ChargeZapFist : BaseChargeFist
{
	protected override bool ShouldKeepChargingAuthority()
	{
		return base.fixedAge < base.chargeDuration;
	}

	protected override EntityState GetNextStateAuthority()
	{
		return new SwingZapFist
		{
			charge = base.charge
		};
	}
}
