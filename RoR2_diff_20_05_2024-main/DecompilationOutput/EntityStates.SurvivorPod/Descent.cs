using RoR2;
using UnityEngine;

namespace EntityStates.SurvivorPod;

public class Descent : SurvivorPodBaseState
{
	public static float failsafeTimer = 30f;

	private ShakeEmitter shakeEmitter;

	private static int InitialSpawnStateHash = Animator.StringToHash("InitialSpawn");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", InitialSpawnStateHash);
		Transform modelTransform = GetModelTransform();
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			Transform transform = component.FindChild("Travel");
			if ((bool)transform)
			{
				shakeEmitter = transform.gameObject.AddComponent<ShakeEmitter>();
				shakeEmitter.wave = new Wave
				{
					amplitude = 1f,
					frequency = 180f,
					cycleOffset = 0f
				};
				shakeEmitter.duration = 10000f;
				shakeEmitter.radius = 400f;
				shakeEmitter.amplitudeTimeDecay = false;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			AuthorityFixedUpdate();
		}
	}

	protected void AuthorityFixedUpdate()
	{
		if (base.fixedAge > failsafeTimer)
		{
			Debug.LogError($"Descent timeout {failsafeTimer}...");
			TransitionIntoNextState();
			return;
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex("Base");
			if (layerIndex != -1 && modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName("Idle"))
			{
				TransitionIntoNextState();
			}
		}
	}

	protected virtual void TransitionIntoNextState()
	{
		outer.SetNextState(new Landed());
	}

	public override void OnExit()
	{
		EntityState.Destroy(shakeEmitter);
		base.OnExit();
	}
}
