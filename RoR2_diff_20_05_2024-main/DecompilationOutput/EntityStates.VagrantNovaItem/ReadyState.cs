using RoR2;

namespace EntityStates.VagrantNovaItem;

public class ReadyState : BaseVagrantNovaItemState
{
	private HealthComponent attachedHealthComponent;

	public override void OnEnter()
	{
		base.OnEnter();
		attachedHealthComponent = base.attachedBody?.healthComponent;
		SetChargeSparkEmissionRateMultiplier(1f);
		GlobalEventManager.onServerDamageDealt += OnDamaged;
	}

	public override void OnExit()
	{
		base.OnExit();
		GlobalEventManager.onServerDamageDealt -= OnDamaged;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && attachedHealthComponent.isHealthLow)
		{
			outer.SetNextState(new ChargeState());
		}
	}

	private void OnDamaged(DamageReport report)
	{
		if (report.hitLowHealth && (object)report?.victim?.body == base.attachedBody)
		{
			outer.SetNextState(new ChargeState());
		}
	}
}
