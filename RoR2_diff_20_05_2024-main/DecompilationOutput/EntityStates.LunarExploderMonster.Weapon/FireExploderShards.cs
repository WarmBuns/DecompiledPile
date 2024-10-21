using UnityEngine;

namespace EntityStates.LunarExploderMonster.Weapon;

public class FireExploderShards : GenericProjectileBaseState
{
	private static int FireExploderShardsStateHash = Animator.StringToHash("FireExploderShards");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Gesture, Additive", FireExploderShardsStateHash);
		base.characterBody.SetAimTimer(0f);
	}
}
