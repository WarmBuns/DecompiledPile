namespace EntityStates;

public class GenericCharacterVehicleSeated : BaseState
{
	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Vehicle;
	}
}
