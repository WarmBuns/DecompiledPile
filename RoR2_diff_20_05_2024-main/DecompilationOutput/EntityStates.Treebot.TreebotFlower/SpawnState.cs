using RoR2;
using UnityEngine;

namespace EntityStates.Treebot.TreebotFlower;

public class SpawnState : BaseState
{
	public static float duration;

	public static string enterSoundString;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation("Base", SpawnStateHash, SpawnParamHash, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new TreebotFlower2Projectile());
		}
	}
}
