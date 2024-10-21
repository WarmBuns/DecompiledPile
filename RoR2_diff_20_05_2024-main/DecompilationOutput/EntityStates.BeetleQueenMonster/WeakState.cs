using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BeetleQueenMonster;

public class WeakState : BaseState
{
	private float stopwatch;

	private float grubStopwatch;

	public static float weakDuration;

	public static float weakToIdleTransitionDuration;

	public static string weakPointChildString;

	public static int maxGrubCount;

	public static float grubSpawnFrequency;

	public static float grubSpawnDelay;

	private int grubCount;

	private bool beginExitTransition;

	private ChildLocator childLocator;

	private static int WeakEnterStateHash = Animator.StringToHash("WeakEnter");

	public override void OnEnter()
	{
		base.OnEnter();
		grubStopwatch -= grubSpawnDelay;
		if ((bool)base.sfxLocator && base.sfxLocator.barkSound != "")
		{
			Util.PlaySound(base.sfxLocator.barkSound, base.gameObject);
		}
		PlayAnimation("Body", WeakEnterStateHash);
		Transform modelTransform = GetModelTransform();
		if (!modelTransform)
		{
			return;
		}
		childLocator = modelTransform.GetComponent<ChildLocator>();
		if ((bool)childLocator)
		{
			Transform transform = childLocator.FindChild(weakPointChildString);
			if ((bool)transform)
			{
				Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/WeakPointProcEffect"), transform.position, transform.rotation);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float deltaTime = GetDeltaTime();
		stopwatch += deltaTime;
		grubStopwatch += deltaTime;
		if (grubStopwatch >= 1f / grubSpawnFrequency && grubCount < maxGrubCount)
		{
			grubCount++;
			grubStopwatch -= 1f / grubSpawnFrequency;
			if (NetworkServer.active)
			{
				Transform transform = childLocator.FindChild("GrubSpawnPoint" + Random.Range(1, 10));
				if ((bool)transform)
				{
					NetworkServer.Spawn(Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/GrubPack"), transform.transform.position, Random.rotation));
					transform.gameObject.SetActive(value: false);
				}
			}
		}
		if (stopwatch >= weakDuration - weakToIdleTransitionDuration && !beginExitTransition)
		{
			beginExitTransition = true;
			PlayCrossfade("Body", "WeakExit", "WeakExit.playbackRate", weakToIdleTransitionDuration, 0.5f);
		}
		if (stopwatch >= weakDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}
