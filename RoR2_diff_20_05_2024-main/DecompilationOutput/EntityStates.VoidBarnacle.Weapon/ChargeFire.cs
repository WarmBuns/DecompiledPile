using RoR2;
using UnityEngine;

namespace EntityStates.VoidBarnacle.Weapon;

public class ChargeFire : BaseState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public GameObject chargeVfxPrefab;

	[SerializeField]
	public string attackSoundEffect;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateName;

	private float _chargingDuration;

	private float _totalDuration;

	private float _crossFadeDuration;

	private GameObject _chargeVfxInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		_totalDuration = baseDuration / attackSpeedStat;
		_crossFadeDuration = _totalDuration * 0.25f;
		_chargingDuration = _totalDuration - _crossFadeDuration;
		Transform modelTransform = GetModelTransform();
		Util.PlayAttackSpeedSound(attackSoundEffect, base.gameObject, attackSpeedStat);
		if (modelTransform != null)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("MuzzleMouth");
				if ((bool)transform && (bool)chargeVfxPrefab)
				{
					_chargeVfxInstance = Object.Instantiate(chargeVfxPrefab, transform.position, transform.rotation, transform);
					ScaleParticleSystemDuration component2 = _chargeVfxInstance.GetComponent<ScaleParticleSystemDuration>();
					if ((bool)component2)
					{
						component2.newDuration = _totalDuration;
					}
				}
			}
		}
		PlayCrossfade(animationLayerName, animationStateName, animationPlaybackRateName, _chargingDuration, _crossFadeDuration);
	}

	public override void Update()
	{
		base.Update();
		if ((bool)_chargeVfxInstance)
		{
			Ray aimRay = GetAimRay();
			_chargeVfxInstance.transform.forward = aimRay.direction;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= _totalDuration && base.isAuthority)
		{
			Fire nextState = new Fire();
			outer.SetNextState(nextState);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if ((bool)_chargeVfxInstance)
		{
			EntityState.Destroy(_chargeVfxInstance);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
