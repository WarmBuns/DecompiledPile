namespace EntityStates.Chef;

public class RolyPolyWeaponBlockingState : BaseSkillState
{
	private ChefController chefController;

	public override void OnEnter()
	{
		base.OnEnter();
		chefController = base.characterBody.GetComponent<ChefController>();
	}

	public override void Update()
	{
		base.Update();
		if (base.isAuthority && !chefController.blockOtherSkills)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
