using System.Collections.Generic;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Huntress.HuntressWeapon;

public class ThrowGlaive : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject chargePrefab;

	public static GameObject muzzleFlashPrefab;

	public static float smallHopStrength;

	public static float antigravityStrength;

	public static float damageCoefficient = 1.2f;

	public static float damageCoefficientPerBounce = 1.1f;

	public static float glaiveProcCoefficient;

	public static int maxBounceCount;

	public static float glaiveTravelSpeed;

	public static float glaiveBounceRange;

	public static string attackSoundString;

	private float duration;

	private float stopwatch;

	private Animator animator;

	private GameObject chargeEffect;

	private Transform modelTransform;

	private HuntressTracker huntressTracker;

	private ChildLocator childLocator;

	private bool hasTriedToThrowGlaive;

	private bool hasSuccessfullyThrownGlaive;

	private HurtBox initialOrbTarget;

	private EffectManagerHelper _emh_chargeEffect;

	private static int ThrowGlaiveStateHash = Animator.StringToHash("ThrowGlaive");

	private static int ThrowGlaiveParamHash = Animator.StringToHash("ThrowGlaive.playbackRate");

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		stopwatch = 0f;
		animator = null;
		chargeEffect = null;
		modelTransform = null;
		huntressTracker = null;
		childLocator = null;
		hasTriedToThrowGlaive = false;
		hasSuccessfullyThrownGlaive = false;
		initialOrbTarget = null;
		_emh_chargeEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		duration = baseDuration / attackSpeedStat;
		modelTransform = GetModelTransform();
		animator = GetModelAnimator();
		huntressTracker = GetComponent<HuntressTracker>();
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSpeedStat);
		if ((bool)huntressTracker && base.isAuthority)
		{
			initialOrbTarget = huntressTracker.GetTrackingTarget();
		}
		if ((bool)base.characterMotor && smallHopStrength != 0f)
		{
			base.characterMotor.velocity.y = smallHopStrength;
		}
		PlayAnimation("FullBody, Override", ThrowGlaiveStateHash, ThrowGlaiveParamHash, duration);
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
			if ((bool)childLocator)
			{
				Transform transform = childLocator.FindChild("HandR");
				if ((bool)transform && (bool)chargePrefab)
				{
					if (!EffectManager.ShouldUsePooledEffect(chargePrefab))
					{
						chargeEffect = Object.Instantiate(chargePrefab, transform.position, transform.rotation);
					}
					else
					{
						_emh_chargeEffect = EffectManager.GetAndActivatePooledEffect(chargePrefab, transform.position, transform.rotation);
						chargeEffect = _emh_chargeEffect.gameObject;
					}
					chargeEffect.transform.parent = transform;
				}
			}
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
	}

	protected void DestroyChargeEffect()
	{
		if ((bool)chargeEffect)
		{
			if (_emh_chargeEffect != null && _emh_chargeEffect.OwningPool != null)
			{
				_emh_chargeEffect.OwningPool.ReturnObject(_emh_chargeEffect);
			}
			else
			{
				EntityState.Destroy(chargeEffect);
			}
			chargeEffect = null;
			_emh_chargeEffect = null;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		DestroyChargeEffect();
		int layerIndex = animator.GetLayerIndex("Impact");
		if (layerIndex >= 0)
		{
			animator.SetLayerWeight(layerIndex, 1.5f);
			animator.PlayInFixedTime("LightImpact", layerIndex, 0f);
		}
		if (!hasTriedToThrowGlaive)
		{
			FireOrbGlaive();
		}
		if (!hasSuccessfullyThrownGlaive && NetworkServer.active)
		{
			base.skillLocator.secondary.AddOneStock();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (!hasTriedToThrowGlaive && animator.GetFloat("ThrowGlaive.fire") > 0f)
		{
			DestroyChargeEffect();
			FireOrbGlaive();
		}
		base.characterMotor.velocity.y += antigravityStrength * GetDeltaTime() * (1f - stopwatch / duration);
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void FireOrbGlaive()
	{
		if (NetworkServer.active && !hasTriedToThrowGlaive)
		{
			hasTriedToThrowGlaive = true;
			LightningOrb lightningOrb = new LightningOrb();
			lightningOrb.lightningType = LightningOrb.LightningType.HuntressGlaive;
			lightningOrb.damageValue = base.characterBody.damage * damageCoefficient;
			lightningOrb.isCrit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
			lightningOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
			lightningOrb.attacker = base.gameObject;
			lightningOrb.procCoefficient = glaiveProcCoefficient;
			lightningOrb.bouncesRemaining = maxBounceCount;
			lightningOrb.speed = glaiveTravelSpeed;
			lightningOrb.bouncedObjects = new List<HealthComponent>();
			lightningOrb.range = glaiveBounceRange;
			lightningOrb.damageCoefficientPerBounce = damageCoefficientPerBounce;
			HurtBox hurtBox = initialOrbTarget;
			if ((bool)hurtBox)
			{
				hasSuccessfullyThrownGlaive = true;
				Transform transform = childLocator.FindChild("HandR");
				EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, "HandR", transmit: true);
				lightningOrb.origin = transform.position;
				lightningOrb.target = hurtBox;
				OrbManager.instance.AddOrb(lightningOrb);
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		writer.Write(HurtBoxReference.FromHurtBox(initialOrbTarget));
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		initialOrbTarget = reader.ReadHurtBoxReference().ResolveHurtBox();
	}
}
