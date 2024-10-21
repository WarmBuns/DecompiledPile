using UnityEngine;

namespace EntityStates.Captain.Weapon;

public class CallAirstrike1 : CallAirstrikeBase
{
	private static int CallAirstrike1StateHash = Animator.StringToHash("CallAirstrike1");

	public override void OnEnter()
	{
		base.OnEnter();
	}

	public override void OnExit()
	{
		PlayAnimation("Gesture, Override", CallAirstrike1StateHash);
		PlayAnimation("Gesture, Additive", CallAirstrike1StateHash);
		AddRecoil(0f, 0f, -1f, -1f);
		base.OnExit();
	}
}
