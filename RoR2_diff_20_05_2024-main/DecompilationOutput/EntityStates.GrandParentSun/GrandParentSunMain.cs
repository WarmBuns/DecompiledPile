using RoR2;

namespace EntityStates.GrandParentSun;

public class GrandParentSunMain : GrandParentSunBase
{
	private GenericOwnership ownership;

	protected override bool shouldEnableSunController => true;

	protected override float desiredVfxScale => 1f;

	public override void OnEnter()
	{
		base.OnEnter();
		ownership = GetComponent<GenericOwnership>();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && !ownership.ownerObject)
		{
			outer.SetNextState(new GrandParentSunDeath());
		}
	}
}
