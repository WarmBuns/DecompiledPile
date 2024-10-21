namespace EntityStates.TitanMonster;

public class ChargeGoldMegaLaser : ChargeMegaLaser
{
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireGoldMegaLaser nextState = new FireGoldMegaLaser();
			outer.SetNextState(nextState);
		}
	}
}
