using EntityStates.Engi.EngiBubbleShield;
using UnityEngine;

namespace EntityStates.Engi.EngiWallShield;

public class Undeployed : EntityStates.Engi.EngiBubbleShield.Undeployed
{
	public override void OnEnter()
	{
		base.OnEnter();
	}

	protected override void SetNextState()
	{
		Vector3 forward = base.transform.forward;
		Vector3 forward2 = new Vector3(forward.x, 0f, forward.z);
		base.transform.forward = forward2;
		outer.SetNextState(new Deployed());
	}
}
