using RoR2;
using UnityEngine;

namespace EntityStates.BrotherMonster;

public class StaggerEnter : StaggerBaseState
{
	public static GameObject effectPrefab;

	public static string effectMuzzleString;

	private static int StaggerEnterStateHash = Animator.StringToHash("StaggerEnter");

	private static int StaggerParamHash = Animator.StringToHash("Stagger.playbackRate");

	public override EntityState nextState => new StaggerLoop();

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", StaggerEnterStateHash, StaggerParamHash, duration);
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, effectMuzzleString, transmit: false);
		}
	}
}
