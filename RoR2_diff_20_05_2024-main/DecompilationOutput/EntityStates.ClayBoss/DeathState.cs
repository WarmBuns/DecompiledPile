using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ClayBoss;

public class DeathState : GenericCharacterDeath
{
	public static GameObject initialEffect;

	public static GameObject deathEffect;

	public static float duration = 2f;

	private float stopwatch;

	private Transform modelBaseTransform;

	private Transform centerTransform;

	private EffectData _effectData;

	private EffectManagerHelper _emh_initialEffect;

	private bool attemptedDeathBehavior;

	public override void Reset()
	{
		base.Reset();
		stopwatch = 0f;
		modelBaseTransform = null;
		centerTransform = null;
		attemptedDeathBehavior = false;
		if (_effectData != null)
		{
			_effectData.Reset();
		}
		_emh_initialEffect = null;
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
					obj.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
					obj.transform.parent = centerTransform;
				}
				else
				{
					_emh_initialEffect = EffectManager.GetAndActivatePooledEffect(initialEffect, centerTransform.position, centerTransform.rotation, centerTransform);
					_emh_initialEffect.gameObject.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
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
		CleanupInitialEffect();
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
		else
		{
			CleanupInitialEffect();
		}
		base.OnExit();
	}

	public void CleanupInitialEffect()
	{
		if (_emh_initialEffect != null && _emh_initialEffect.OwningPool != null)
		{
			_emh_initialEffect.OwningPool.ReturnObject(_emh_initialEffect);
			_emh_initialEffect = null;
		}
	}
}
