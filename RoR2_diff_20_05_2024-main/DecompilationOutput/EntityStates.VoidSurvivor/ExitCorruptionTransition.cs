using EntityStates.VoidSurvivor.CorruptMode;
using UnityEngine.Networking;

namespace EntityStates.VoidSurvivor;

public class ExitCorruptionTransition : CorruptionTransitionBase
{
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)voidSurvivorController && NetworkServer.active)
		{
			voidSurvivorController.AddCorruption(-100f);
		}
	}

	public override void OnFinishAuthority()
	{
		base.OnFinishAuthority();
		if ((bool)voidSurvivorController)
		{
			voidSurvivorController.corruptionModeStateMachine.SetNextState(new UncorruptedMode());
		}
	}
}
