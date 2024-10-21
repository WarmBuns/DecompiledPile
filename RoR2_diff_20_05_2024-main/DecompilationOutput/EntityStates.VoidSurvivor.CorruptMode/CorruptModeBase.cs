using RoR2;

namespace EntityStates.VoidSurvivor.CorruptMode;

public class CorruptModeBase : BaseState
{
	protected VoidSurvivorController voidSurvivorController;

	public override void OnEnter()
	{
		base.OnEnter();
		voidSurvivorController = GetComponent<VoidSurvivorController>();
	}
}
