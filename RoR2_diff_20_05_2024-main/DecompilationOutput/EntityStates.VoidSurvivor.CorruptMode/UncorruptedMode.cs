namespace EntityStates.VoidSurvivor.CorruptMode;

public class UncorruptedMode : CorruptModeBase
{
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && (bool)voidSurvivorController && voidSurvivorController.corruption >= voidSurvivorController.maxCorruption && (bool)voidSurvivorController.bodyStateMachine)
		{
			voidSurvivorController.bodyStateMachine.SetInterruptState(new EnterCorruptionTransition(), InterruptPriority.Skill);
		}
	}
}
