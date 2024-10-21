using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.RoboBallBoss;

public class DeathState : GenericCharacterDeath
{
	public static GameObject initialEffect;

	public static GameObject deathEffect;

	public static float duration = 2f;

	private float stopwatch;

	private Transform modelBaseTransform;

	private Transform centerTransform;

	private bool attemptedDeathBehavior;

	public override void OnEnter()
	{
		base.OnEnter();
		if (!base.modelLocator)
		{
			return;
		}
		ChildLocator component = base.modelLocator.modelTransform.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			centerTransform = component.FindChild("Center");
			if ((bool)initialEffect)
			{
				GameObject obj = Object.Instantiate(initialEffect, centerTransform.position, centerTransform.rotation);
				obj.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
				obj.transform.parent = centerTransform;
			}
		}
		modelBaseTransform = base.modelLocator.modelBaseTransform;
	}

	private void AttemptDeathBehavior()
	{
		if (!attemptedDeathBehavior)
		{
			attemptedDeathBehavior = true;
			if ((bool)deathEffect && NetworkServer.active && (bool)centerTransform)
			{
				EffectManager.SpawnEffect(deathEffect, new EffectData
				{
					origin = centerTransform.position
				}, transmit: true);
			}
			if ((bool)modelBaseTransform)
			{
				EntityState.Destroy(modelBaseTransform.gameObject);
				modelBaseTransform = null;
			}
			if (NetworkServer.active)
			{
				EntityState.Destroy(base.gameObject);
			}
		}
	}

	public override void FixedUpdate()
	{
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration)
		{
			AttemptDeathBehavior();
		}
	}

	public override void OnExit()
	{
		if (!outer.destroying)
		{
			AttemptDeathBehavior();
		}
		base.OnExit();
	}
}
