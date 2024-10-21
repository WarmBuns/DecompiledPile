namespace EntityStates.VoidSurvivor.Weapon;

public class ChargeCrushHealth : ChargeCrushBase
{
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextState(new CrushHealth());
		}
	}
}
