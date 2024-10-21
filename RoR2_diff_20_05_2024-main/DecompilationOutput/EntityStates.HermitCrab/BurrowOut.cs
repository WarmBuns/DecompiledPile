using RoR2;
using UnityEngine;

namespace EntityStates.HermitCrab;

public class BurrowOut : BaseState
{
	public static GameObject burrowPrefab;

	public static float baseDuration;

	public static string burrowOutSoundString;

	private float stopwatch;

	private Transform modelTransform;

	private ChildLocator childLocator;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("Body", "BurrowOut", "BurrowOut.playbackRate", duration, 0.1f);
		modelTransform = GetModelTransform();
		childLocator = modelTransform.GetComponent<ChildLocator>();
		Util.PlaySound(burrowOutSoundString, base.gameObject);
		EffectManager.SimpleMuzzleFlash(burrowPrefab, base.gameObject, "BurrowCenter", transmit: false);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
