using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GreaterWispMonster;

public class DeathState : GenericCharacterDeath
{
	[SerializeField]
	public GameObject initialEffect;

	[SerializeField]
	public GameObject deathEffect;

	private static float duration = 2f;

	private GameObject initialEffectInstance;

	private bool spawnedEffect;

	private EffectData _effectData;

	protected EffectManagerHelper _efh_initializeEffectInstance;

	public override void Reset()
	{
		base.Reset();
		initialEffectInstance = null;
		spawnedEffect = false;
		if (_effectData != null)
		{
			_effectData.Reset();
		}
		_efh_initializeEffectInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if (!base.modelLocator)
		{
			return;
		}
		ChildLocator component = base.modelLocator.modelTransform.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		Transform transform = component.FindChild("Mask");
		transform.gameObject.SetActive(value: true);
		transform.GetComponent<AnimateShaderAlpha>().timeMax = duration;
		if ((bool)initialEffect)
		{
			if (!EffectManager.ShouldUsePooledEffect(initialEffect))
			{
				initialEffectInstance = Object.Instantiate(initialEffect, transform.position, transform.rotation, transform);
				return;
			}
			_efh_initializeEffectInstance = EffectManager.GetAndActivatePooledEffect(initialEffect, transform.position, transform.rotation);
			initialEffectInstance = _efh_initializeEffectInstance.gameObject;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && NetworkServer.active && (bool)deathEffect && !spawnedEffect)
		{
			spawnedEffect = true;
			if (_effectData == null)
			{
				_effectData = new EffectData();
			}
			_effectData.origin = base.transform.position;
			EffectManager.SpawnEffect(deathEffect, _effectData, transmit: true);
			if (base.isAuthority)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		Debug.LogError("gwisp death onexit");
		EntityState.Destroy(base.gameObject);
		if (!initialEffectInstance)
		{
			return;
		}
		if (!EffectManager.UsePools)
		{
			EntityState.Destroy(initialEffectInstance);
			return;
		}
		if (_efh_initializeEffectInstance != null && _efh_initializeEffectInstance.OwningPool != null)
		{
			_efh_initializeEffectInstance.OwningPool.ReturnObject(_efh_initializeEffectInstance);
		}
		else
		{
			if (_efh_initializeEffectInstance != null)
			{
				Debug.LogFormat("GreaterWispDeathState has no owning pool {0} {1}", base.gameObject.name, base.gameObject.GetInstanceID());
			}
			EntityState.Destroy(initialEffectInstance);
		}
		initialEffectInstance = null;
		_efh_initializeEffectInstance = null;
	}
}
