using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BeetleGuardMonster;

public class GroundSlam : BaseState
{
	public static float baseDuration = 3.5f;

	public static float damageCoefficient = 4f;

	public static float forceMagnitude = 16f;

	private OverlapAttack attack;

	public static string initialAttackSoundString;

	public static GameObject chargeEffectPrefab;

	public static GameObject slamEffectPrefab;

	public static GameObject hitEffectPrefab;

	private Animator modelAnimator;

	private Transform modelTransform;

	private bool hasAttacked;

	private float duration;

	private GameObject leftHandChargeEffect;

	private GameObject rightHandChargeEffect;

	private ChildLocator modelChildLocator;

	private EffectManagerHelper _emh_leftHandChargeEffect;

	private EffectManagerHelper _emh_rightHandChargeEffect;

	private Transform groundSlamIndicatorInstance;

	public override void Reset()
	{
		base.Reset();
		if (attack != null)
		{
			attack.Reset();
		}
		modelAnimator = null;
		modelTransform = null;
		hasAttacked = false;
		duration = 0f;
		leftHandChargeEffect = null;
		rightHandChargeEffect = null;
		modelChildLocator = null;
		_emh_leftHandChargeEffect = null;
		_emh_rightHandChargeEffect = null;
		groundSlamIndicatorInstance = null;
	}

	private void EnableIndicator(Transform indicator)
	{
		if ((bool)indicator)
		{
			indicator.gameObject.SetActive(value: true);
			ObjectScaleCurve component = indicator.gameObject.GetComponent<ObjectScaleCurve>();
			if ((bool)component)
			{
				component.time = 0f;
			}
		}
	}

	private void DisableIndicator(Transform indicator)
	{
		if ((bool)indicator)
		{
			indicator.gameObject.SetActive(value: false);
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		modelTransform = GetModelTransform();
		Util.PlaySound(initialAttackSoundString, base.gameObject);
		_ = (bool)base.characterDirection;
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = damageCoefficient * damageStat;
		attack.hitEffectPrefab = hitEffectPrefab;
		attack.forceVector = Vector3.up * forceMagnitude;
		if ((bool)modelTransform)
		{
			attack.hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "GroundSlam");
		}
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("Body", "GroundSlam", "GroundSlam.playbackRate", duration, 0.2f);
		if (!modelTransform)
		{
			return;
		}
		modelChildLocator = modelTransform.GetComponent<ChildLocator>();
		if (!modelChildLocator)
		{
			return;
		}
		GameObject gameObject = chargeEffectPrefab;
		Transform transform = modelChildLocator.FindChild("HandL");
		Transform transform2 = modelChildLocator.FindChild("HandR");
		if ((bool)transform)
		{
			if (!EffectManager.ShouldUsePooledEffect(gameObject))
			{
				leftHandChargeEffect = UnityEngine.Object.Instantiate(gameObject, transform);
			}
			else
			{
				_emh_leftHandChargeEffect = EffectManager.GetAndActivatePooledEffect(gameObject, transform, inResetLocal: true);
				leftHandChargeEffect = _emh_leftHandChargeEffect.gameObject;
			}
		}
		if ((bool)transform2)
		{
			if (!EffectManager.ShouldUsePooledEffect(gameObject))
			{
				rightHandChargeEffect = UnityEngine.Object.Instantiate(gameObject, transform2);
			}
			else
			{
				_emh_rightHandChargeEffect = EffectManager.GetAndActivatePooledEffect(gameObject, transform2, inResetLocal: true);
				rightHandChargeEffect = _emh_rightHandChargeEffect.gameObject;
			}
		}
		groundSlamIndicatorInstance = modelChildLocator.FindChild("GroundSlamIndicator");
		EnableIndicator(groundSlamIndicatorInstance);
	}

	protected void DestroyChargeEffects()
	{
		if (_emh_leftHandChargeEffect != null && _emh_leftHandChargeEffect.OwningPool != null)
		{
			_emh_leftHandChargeEffect.OwningPool.ReturnObject(_emh_leftHandChargeEffect);
		}
		else
		{
			EntityState.Destroy(leftHandChargeEffect);
		}
		leftHandChargeEffect = null;
		_emh_leftHandChargeEffect = null;
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

	public override void OnExit()
	{
		DestroyChargeEffects();
		DisableIndicator(groundSlamIndicatorInstance);
		_ = (bool)base.characterDirection;
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)modelAnimator && modelAnimator.GetFloat("GroundSlam.hitBoxActive") > 0.5f && !hasAttacked)
		{
			if (NetworkServer.active)
			{
				attack.Fire();
			}
			if (base.isAuthority && (bool)modelTransform)
			{
				DisableIndicator(groundSlamIndicatorInstance);
				EffectManager.SimpleMuzzleFlash(slamEffectPrefab, base.gameObject, "SlamZone", transmit: true);
			}
			hasAttacked = true;
			DestroyChargeEffects();
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
