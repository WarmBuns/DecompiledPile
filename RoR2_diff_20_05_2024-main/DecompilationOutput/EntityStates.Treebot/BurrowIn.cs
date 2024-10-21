using RoR2;
using UnityEngine;

namespace EntityStates.Treebot;

public class BurrowIn : BaseState
{
	public static GameObject burrowPrefab;

	public static float baseDuration;

	public static string burrowInSoundString;

	private float stopwatch;

	private float duration;

	private Transform modelTransform;

	private ChildLocator childLocator;

	private CameraTargetParams.AimRequest aimRequest;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("Body", "BurrowIn", "BurrowIn.playbackRate", duration, 0.1f);
		modelTransform = GetModelTransform();
		childLocator = modelTransform.GetComponent<ChildLocator>();
		Util.PlaySound(burrowInSoundString, base.gameObject);
		EffectManager.SimpleMuzzleFlash(burrowPrefab, base.gameObject, "BurrowCenter", transmit: false);
		if ((bool)base.characterBody)
		{
			base.characterBody.hideCrosshair = true;
		}
		if ((bool)base.cameraTargetParams)
		{
			aimRequest = base.cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
		}
	}

	public override void OnExit()
	{
		aimRequest?.Dispose();
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
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
