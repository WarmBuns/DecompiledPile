using RoR2.VoidRaidCrab;

namespace EntityStates.VoidRaidCrab;

public class WaitForLegsAvailable : BaseState
{
	public EntityState nextState;

	private CentralLegController centralLegController;

	public override void OnEnter()
	{
		base.OnEnter();
		centralLegController = GetComponent<CentralLegController>();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && !centralLegController.AreLegsBlockingBodyAnimation())
		{
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return nextState.GetMinimumInterruptPriority();
	}
}
