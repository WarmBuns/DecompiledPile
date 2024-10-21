using RoR2;

namespace EntityStates.Loader;

public class BeginOvercharge : BaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			GetComponent<LoaderStaticChargeComponent>().ConsumeChargeAuthority();
		}
		outer.SetNextStateToMain();
	}
}
