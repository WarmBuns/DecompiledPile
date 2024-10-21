using UnityEngine;

namespace EntityStates;

public class TestState2 : EntityState
{
	public override void OnEnter()
	{
		Debug.LogFormat("{0} Entering TestState2.", base.gameObject);
	}

	public override void OnExit()
	{
		Debug.LogFormat("{0} Exiting TestState2.", base.gameObject);
	}

	public override void FixedUpdate()
	{
		if (base.isAuthority && Input.GetButton("Fire2"))
		{
			outer.SetNextState(new TestState1());
		}
	}
}
