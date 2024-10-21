using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.TitanMonster;

public class DeathState : GenericCharacterDeath
{
	public static GameObject initialEffect;

	[SerializeField]
	public GameObject deathEffect;

	public static float duration = 2f;

	private float stopwatch;

	private Transform centerTransform;

	private Transform modelBaseTransform;

	private bool attemptedDeathBehavior;

	private EffectData _effectData;

	public override void Reset()
	{
		base.Reset();
		centerTransform = null;
		modelBaseTransform = null;
		attemptedDeathBehavior = false;
		if (_effectData != null)
		{
			_effectData.Reset();
		}
	}

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
				if (!EffectManager.ShouldUsePooledEffect(initialEffect))
				{
					GameObject obj = Object.Instantiate(initialEffect, centerTransform.position, centerTransform.rotation);
					obj.GetComponent<ScaleParticleSystemDuration>().newDuration = duration + 2f;
					obj.transform.parent = centerTransform;
				}
				else
				{
					EffectManager.GetAndActivatePooledEffect(initialEffect, centerTransform.position, centerTransform.rotation, centerTransform).gameObject.GetComponent<ScaleParticleSystemDuration>().newDuration = duration + 2f;
				}
			}
		}
		modelBaseTransform = base.modelLocator.modelBaseTransform;
	}

	private void AttemptDeathBehavior()
	{
		if (attemptedDeathBehavior)
		{
			return;
		}
		attemptedDeathBehavior = true;
		if ((bool)deathEffect && NetworkServer.active)
		{
			if (_effectData == null)
			{
				_effectData = new EffectData();
			}
			_effectData.origin = centerTransform.position;
			EffectManager.SpawnEffect(deathEffect, _effectData, transmit: true);
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
