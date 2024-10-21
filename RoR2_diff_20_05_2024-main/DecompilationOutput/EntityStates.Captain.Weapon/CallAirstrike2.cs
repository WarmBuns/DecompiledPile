using UnityEngine;

namespace EntityStates.Captain.Weapon;

public class CallAirstrike2 : CallAirstrikeBase
{
	private static int CallAirstrike2StateHash = Animator.StringToHash("CallAirstrike2");

	public override void OnExit()
	{
		PlayAnimation("Gesture, Override", CallAirstrike2StateHash);
		PlayAnimation("Gesture, Additive", CallAirstrike2StateHash);
		AddRecoil(0f, 0f, 1f, 1f);
		base.OnExit();
	}
}
