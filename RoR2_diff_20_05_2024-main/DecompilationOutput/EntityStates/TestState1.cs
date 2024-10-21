using UnityEngine;

namespace EntityStates;

public class TestState1 : EntityState
{
	public override void OnEnter()
	{
		Debug.LogFormat("{0} Entering TestState1.", base.gameObject);
	}

	public override void OnExit()
	{
		Debug.LogFormat("{0} Exiting TestState1.", base.gameObject);
	}

	public override void FixedUpdate()
	{
		if (base.isAuthority && Input.GetButton("Fire1"))
		{
			outer.SetNextState(new TestState2());
		}
	}
}
