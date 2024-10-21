using RoR2;
using UnityEngine;

namespace EntityStates.VoidJailer.Weapon;

public class ChargeFire : BaseState
{
	public static string attackSoundEffect;

	public static string animationLayerName;

	public static string animationStateName;

	public static string animationPlaybackRateName;

	public static float baseDuration;

	public static GameObject chargeVfxPrefab;

	private float _totalDuration;

	private float _crossFadeDuration;

	private float _chargingDuration;

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
				Transform transform = component.FindChild("ClawMuzzle");
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
		base.characterBody.SetAimTimer(_totalDuration + 3f);
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
		return InterruptPriority.PrioritySkill;
	}
}
