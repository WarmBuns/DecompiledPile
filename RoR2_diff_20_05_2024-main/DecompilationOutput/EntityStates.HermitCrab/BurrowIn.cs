using RoR2;
using UnityEngine;

namespace EntityStates.HermitCrab;

public class BurrowIn : BaseState
{
	public static GameObject burrowPrefab;

	public static float baseDuration;

	public static string burrowInSoundString;

	private float stopwatch;

	private float duration;

	private Transform modelTransform;

	private ChildLocator childLocator;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("Body", "BurrowIn", "BurrowIn.playbackRate", duration, 0.1f);
		modelTransform = GetModelTransform();
		childLocator = modelTransform.GetComponent<ChildLocator>();
		Util.PlaySound(burrowInSoundString, base.gameObject);
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
			Burrowed nextState = new Burrowed();
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
