namespace EntityStates;

public class IdleSkillOverrideHandoff : EntityState, ISkillOverrideHandoff
{
	public static float skillOverrideTimeout = 0.05f;

	private bool shouldProcess;

	private SkillStateOverrideData skillOverrideData;

	public void TransferSkillOverride(SkillStateOverrideData skillOverrideData)
	{
		this.skillOverrideData = skillOverrideData;
		shouldProcess = this.skillOverrideData != null;
	}

	public override void Update()
	{
		if (shouldProcess)
		{
			base.Update();
			skillOverrideData.StepRestock();
			if (base.age > skillOverrideTimeout)
			{
				skillOverrideData.ClearOverrides();
				skillOverrideData = null;
				shouldProcess = false;
			}
		}
	}

	public override void ModifyNextState(EntityState nextState)
	{
		base.ModifyNextState(nextState);
		if (skillOverrideData != null && nextState is ISkillOverrideHandoff skillOverrideHandoff)
		{
			skillOverrideHandoff.TransferSkillOverride(skillOverrideData);
			skillOverrideData = null;
			shouldProcess = false;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if (shouldProcess)
		{
			skillOverrideData?.ClearOverrides();
		}
	}
}
