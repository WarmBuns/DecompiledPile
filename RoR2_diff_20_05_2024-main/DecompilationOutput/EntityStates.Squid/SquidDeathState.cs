using UnityEngine;

namespace EntityStates.Squid;

public class SquidDeathState : GenericCharacterDeath
{
	private static int DeathStateHash = Animator.StringToHash("Death");

	public static float duration = 0f;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", DeathStateHash);
	}
}
