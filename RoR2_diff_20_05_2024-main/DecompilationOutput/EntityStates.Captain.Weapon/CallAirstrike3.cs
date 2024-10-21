using UnityEngine;

namespace EntityStates.Captain.Weapon;

public class CallAirstrike3 : CallAirstrikeBase
{
	private static int CallAirstrike3StateHash = Animator.StringToHash("CallAirstrike3");

	public override void OnExit()
	{
		PlayAnimation("Gesture, Override", CallAirstrike3StateHash);
		AddRecoil(-2f, -2f, -0.5f, 0.5f);
		base.OnExit();
	}
}
