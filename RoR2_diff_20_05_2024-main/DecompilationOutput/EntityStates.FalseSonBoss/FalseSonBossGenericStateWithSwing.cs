namespace EntityStates.FalseSonBoss;

public class FalseSonBossGenericStateWithSwing : GenericCharacterMain
{
	private bool transitioningToMeleeSwingState;

	public override void OnEnter()
	{
		base.OnEnter();
		transitioningToMeleeSwingState = false;
	}

	public override void Update()
	{
		if (base.characterBody == null || base.characterBody.coreTransform == null)
		{
			if (outer != null)
			{
				outer.SetNextStateToMain();
			}
			return;
		}
		base.Update();
		if (!transitioningToMeleeSwingState)
		{
			FalseSonBossStateHelper.TrySwitchToMeleeSwing(ref transitioningToMeleeSwingState, base.characterBody.coreTransform, base.characterDirection.forward, null, outer);
		}
	}

	public override void FixedUpdate()
	{
		if (!transitioningToMeleeSwingState)
		{
			base.FixedUpdate();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		if (transitioningToMeleeSwingState)
		{
			return InterruptPriority.Frozen;
		}
		return InterruptPriority.Any;
	}
}
