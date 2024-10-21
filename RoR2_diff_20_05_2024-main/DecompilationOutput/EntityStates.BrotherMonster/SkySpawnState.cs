using RoR2;
using UnityEngine;

namespace EntityStates.BrotherMonster;

public class SkySpawnState : BaseState
{
	public static float duration;

	public static string soundString;

	private static int ExitSkyLeapStateHash = Animator.StringToHash("ExitSkyLeap");

	private static int BufferEmptyLeapStateHash = Animator.StringToHash("BufferEmpty");

	private static int SkyLeapParamHash = Animator.StringToHash("SkyLeap.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", ExitSkyLeapStateHash, SkyLeapParamHash, duration);
		PlayAnimation("FullBody Override", BufferEmptyLeapStateHash);
		Util.PlaySound(soundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextStateToMain();
		}
	}
}
