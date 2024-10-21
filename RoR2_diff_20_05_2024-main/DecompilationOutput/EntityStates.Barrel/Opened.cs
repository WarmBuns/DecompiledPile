using UnityEngine;

namespace EntityStates.Barrel;

public class Opened : EntityState
{
	private static int OpenedStateHash = Animator.StringToHash("Opened");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", OpenedStateHash);
		SetPingable(value: false);
	}
}
