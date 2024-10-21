using RoR2;

namespace EntityStates.SiphonItem;

public class ReadyState : BaseSiphonItemState
{
	public static float healthFractionThreshold = 0.5f;

	private HealthComponent attachedHealthComponent;

	public override void OnEnter()
	{
		base.OnEnter();
		attachedHealthComponent = base.attachedBody?.healthComponent;
		TurnOffHealingFX();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && attachedHealthComponent.combinedHealthFraction <= healthFractionThreshold)
		{
			outer.SetNextState(new ChargeState());
		}
	}
}
