using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.BeetleGuardMonster;

public class FireSunder : BaseState
{
	public static float baseDuration = 3.5f;

	public static float damageCoefficient = 4f;

	public static float forceMagnitude = 16f;

	public static string initialAttackSoundString;

	public static GameObject chargeEffectPrefab;

	public static GameObject projectilePrefab;

	public static GameObject hitEffectPrefab;

	private Animator modelAnimator;

	private Transform modelTransform;

	private bool hasAttacked;

	private float duration;

	private GameObject rightHandChargeEffect;

	private ChildLocator modelChildLocator;

	private Transform handRTransform;

	private EffectManagerHelper _emh_rightHandChargeEffect;

	public override void Reset()
	{
		base.Reset();
		modelAnimator = null;
		modelTransform = null;
		hasAttacked = false;
		duration = 0f;
		rightHandChargeEffect = null;
		modelChildLocator = null;
		handRTransform = null;
		_emh_rightHandChargeEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		modelTransform = GetModelTransform();
		Util.PlaySound(initialAttackSoundString, base.gameObject);
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("Body", "FireSunder", "FireSunder.playbackRate", duration, 0.2f);
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration + 2f);
		}
		if (!modelTransform)
		{
			return;
		}
		AimAnimator component = modelTransform.GetComponent<AimAnimator>();
		if ((bool)component)
		{
			component.enabled = true;
		}
		modelChildLocator = modelTransform.GetComponent<ChildLocator>();
		if (!modelChildLocator)
		{
			return;
		}
		GameObject gameObject = chargeEffectPrefab;
		handRTransform = modelChildLocator.FindChild("HandR");
		if ((bool)handRTransform)
		{
			if (!EffectManager.ShouldUsePooledEffect(gameObject))
			{
				rightHandChargeEffect = Object.Instantiate(gameObject, handRTransform);
				return;
			}
			_emh_rightHandChargeEffect = EffectManager.GetAndActivatePooledEffect(gameObject, handRTransform, inResetLocal: true);
			rightHandChargeEffect = _emh_rightHandChargeEffect.gameObject;
		}
	}

	public override void OnExit()
	{
		DestroyRightHandCharge();
		if ((bool)modelTransform)
		{
			AimAnimator component = modelTransform.GetComponent<AimAnimator>();
			if ((bool)component)
			{
				component.enabled = true;
			}
		}
		base.OnExit();
	}

	public void DestroyRightHandCharge()
	{
		if (_emh_rightHandChargeEffect != null && _emh_rightHandChargeEffect.OwningPool != null)
		{
			_emh_rightHandChargeEffect.OwningPool.ReturnObject(_emh_rightHandChargeEffect);
		}
		else
		{
			EntityState.Destroy(rightHandChargeEffect);
		}
		rightHandChargeEffect = null;
		_emh_rightHandChargeEffect = null;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)modelAnimator && modelAnimator.GetFloat("FireSunder.activate") > 0.5f && !hasAttacked)
		{
			if (base.isAuthority && (bool)modelTransform)
			{
				Ray aimRay = GetAimRay();
				aimRay.origin = handRTransform.position;
				ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, forceMagnitude, Util.CheckRoll(critStat, base.characterBody.master));
			}
			hasAttacked = true;
			DestroyRightHandCharge();
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
