using System;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Chef;

public class RolyPoly : GenericCharacterMain
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float speedMultiplier;

	[SerializeField]
	public float speedMultiplierLvlUp;

	[SerializeField]
	public float baseDurationLvlUp;

	public static float chargeDamageCoefficient;

	public static float chargeDamageCoefficientLvlUp;

	public static float awayForceMagnitude;

	public static float upwardForceMagnitude;

	public static GameObject impactEffectPrefab;

	public static GameObject impactEffectBoostedPrefab;

	public static float hitPauseDuration;

	public static string impactSoundString;

	public static float recoilAmplitude;

	public static string startSoundString;

	public static string endSoundString;

	public static GameObject knockbackEffectPrefab;

	public static float knockbackDamageCoefficient;

	public static float massThresholdForKnockback;

	public static float knockbackForce;

	public static float knockbackForceLvlUp;

	[SerializeField]
	public GameObject startEffectPrefab;

	[SerializeField]
	public GameObject midEffectPrefab;

	[SerializeField]
	public GameObject endEffectPrefab;

	private uint soundID;

	private float duration;

	private float hitPauseTimer;

	private Vector3 idealDirection;

	private OverlapAttack attack;

	private bool inHitPause;

	private int chestIndex = -1;

	private GameObject temporaryInstantiationTransform;

	private GameObject midEffectInstance;

	private float chargeDamageCoefficientTemp;

	private float speedMultiplierTemp;

	private float knockBackForceTemp;

	private float baseDurationTemp;

	private ChefController chefController;

	[SerializeField]
	public bool hasBoost;

	[SerializeField]
	public float charge;

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public float whirlwindDamageCo;

	private int originalLayer;

	[SerializeField]
	public GameObject rollerSpikePrefab;

	private GameObject rollerSpikeInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		chefController = base.characterBody.GetComponent<ChefController>();
		chefController.rolyPolyActive = true;
		originalLayer = base.gameObject.layer;
		base.gameObject.layer = LayerIndex.GetAppropriateFakeLayerForTeam(base.teamComponent.teamIndex).intVal;
		base.characterMotor?.Motor.RebuildCollidableLayers();
		chargeDamageCoefficientTemp = chargeDamageCoefficient;
		knockBackForceTemp = knockbackForce;
		speedMultiplierTemp = speedMultiplier;
		baseDurationTemp = baseDuration;
		float num = charge;
		if (num <= 1f)
		{
			if (num != 0f && num == 1f)
			{
				chargeDamageCoefficientTemp += chargeDamageCoefficientTemp * chargeDamageCoefficientLvlUp;
				speedMultiplierTemp += speedMultiplierTemp * speedMultiplierLvlUp;
				knockBackForceTemp += knockBackForceTemp * knockbackForceLvlUp;
				baseDurationTemp += baseDurationLvlUp;
			}
		}
		else if (num != 2f)
		{
			if (num == 3f)
			{
				chargeDamageCoefficientTemp += chargeDamageCoefficientTemp * (chargeDamageCoefficientLvlUp * 3f);
				speedMultiplierTemp += speedMultiplierTemp * (speedMultiplierLvlUp * 3f);
				knockBackForceTemp += knockBackForceTemp * (knockbackForceLvlUp * 3f);
				baseDurationTemp += baseDurationLvlUp * 3f;
			}
		}
		else
		{
			chargeDamageCoefficientTemp += chargeDamageCoefficientTemp * (chargeDamageCoefficientLvlUp * 2f);
			speedMultiplierTemp += speedMultiplierTemp * (speedMultiplierLvlUp * 2f);
			knockBackForceTemp += knockBackForceTemp * (knockbackForceLvlUp * 2f);
			baseDurationTemp += baseDurationLvlUp * 2f;
		}
		if (NetworkServer.active)
		{
			base.characterBody.AddBuff(DLC2Content.Buffs.CookingRolling);
		}
		if (hasBoost)
		{
			Util.PlaySound("Play_chef_skill3_start", base.gameObject);
			Util.PlaySound("Play_chef_skill3_boosted_active_loop", base.gameObject);
			ProjectileManager.instance.FireProjectile(projectilePrefab, base.gameObject.transform.position, Quaternion.identity, base.gameObject, base.characterBody.damage * whirlwindDamageCo, 0f, Util.CheckRoll(critStat, base.characterBody.master));
			PlayAnimation("Body", "BoostedRolyPoly", "BoostedRolyPoly.playbackRate", 1f);
			GetModelAnimator().SetBool("isInBoostedRolyPoly", value: true);
		}
		else
		{
			Util.PlaySound("Play_chef_skill3_start", base.gameObject);
			GetModelAnimator().SetBool("isInRolyPoly", value: true);
			PlayAnimation("Body", "FireRolyPoly", "FireRolyPoly.playbackRate", 1f);
		}
		duration = baseDurationTemp;
		if (base.isAuthority)
		{
			if ((bool)base.inputBank)
			{
				idealDirection = base.inputBank.aimDirection;
				idealDirection.y = 0f;
			}
			UpdateDirection();
		}
		temporaryInstantiationTransform = GetModelTransform().gameObject;
		if ((bool)base.modelLocator)
		{
			base.modelLocator.normalizeToFloor = true;
		}
		if ((bool)base.characterBody)
		{
			if ((bool)startEffectPrefab)
			{
				EffectManager.SpawnEffect(startEffectPrefab, new EffectData
				{
					origin = base.characterBody.corePosition
				}, transmit: false);
			}
			if ((bool)midEffectPrefab && (bool)temporaryInstantiationTransform)
			{
				midEffectInstance = UnityEngine.Object.Instantiate(midEffectPrefab, temporaryInstantiationTransform.transform);
			}
		}
		if ((bool)base.characterDirection)
		{
			base.characterDirection.forward = idealDirection;
		}
		Util.PlaySound("Stop_chef_skill3_charge_loop", base.gameObject);
		HitBoxGroup hitBoxGroup = null;
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "Charge");
		}
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = GetTeam();
		attack.damage = chargeDamageCoefficientTemp * damageStat;
		attack.hitEffectPrefab = (hasBoost ? impactEffectBoostedPrefab : impactEffectPrefab);
		attack.forceVector = Vector3.up * upwardForceMagnitude;
		attack.pushAwayForce = knockBackForceTemp;
		attack.hitBoxGroup = hitBoxGroup;
		attack.isCrit = RollCrit();
		attack.damageType = DamageType.Stun1s;
		attack.retriggerTimeout = 0.5f;
	}

	private void HandleSpawnRollerspikes()
	{
		if (!rollerSpikePrefab)
		{
			return;
		}
		ChildLocator modelChildLocator = GetModelChildLocator();
		if ((bool)modelChildLocator)
		{
			GameObject gameObject = modelChildLocator.FindChildGameObject("Wheel");
			if ((bool)gameObject)
			{
				rollerSpikeInstance = UnityEngine.Object.Instantiate(rollerSpikePrefab, gameObject.transform);
			}
		}
	}

	private void HandleCleanupRollerSpikes()
	{
		if ((bool)rollerSpikeInstance)
		{
			EntityState.Destroy(rollerSpikeInstance);
		}
	}

	public override void OnExit()
	{
		chefController.rolyPolyActive = false;
		chefController.SetYesChefHeatState(newYesChefHeatState: false);
		chefController.blockOtherSkills = false;
		if (hasBoost)
		{
			if (NetworkServer.active && base.characterBody.HasBuff(DLC2Content.Buffs.boostedFireEffect))
			{
				base.characterBody.RemoveBuff(DLC2Content.Buffs.boostedFireEffect);
			}
			if (base.isAuthority)
			{
				chefController.ClearSkillOverrides();
			}
		}
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(DLC2Content.Buffs.CookingRolling);
		}
		base.gameObject.layer = originalLayer;
		base.characterMotor?.Motor.RebuildCollidableLayers();
		HandleCleanupRollerSpikes();
		if (!outer.destroying && (bool)base.characterBody)
		{
			if ((bool)endEffectPrefab)
			{
				EffectManager.SpawnEffect(endEffectPrefab, new EffectData
				{
					origin = base.characterBody.corePosition
				}, transmit: false);
			}
			if ((bool)midEffectInstance)
			{
				EntityState.Destroy(midEffectInstance);
			}
			base.characterBody.isSprinting = false;
			if (NetworkServer.active)
			{
				base.characterBody.RemoveBuff(RoR2Content.Buffs.ArmorBoost);
			}
		}
		if ((bool)base.characterMotor && !base.characterMotor.disableAirControlUntilCollision)
		{
			base.characterMotor.velocity += GetIdealVelocity();
		}
		if ((bool)base.modelLocator)
		{
			base.modelLocator.normalizeToFloor = false;
		}
		GetModelAnimator().SetBool("isInRolyPoly", value: false);
		GetModelAnimator().SetBool("isInBoostedRolyPoly", value: false);
		PlayCrossfade("Body", "ExitRolyPoly", 0.1f);
		AkSoundEngine.StopPlayingID(soundID);
		if (hasBoost)
		{
			Util.PlaySound("Stop_chef_skill3_boosted_active_loop", base.gameObject);
		}
		Util.PlaySound(endSoundString, base.gameObject);
		Util.PlaySound("Stop_chef_skill3_active_loop", base.gameObject);
		Util.PlaySound("Stop_chef_skill3_charge_loop", base.gameObject);
		base.OnExit();
	}

	private float GetDamageBoostFromSpeed()
	{
		return Mathf.Max(1f, base.characterBody.moveSpeed / base.characterBody.baseMoveSpeed);
	}

	private void UpdateDirection()
	{
		if ((bool)base.inputBank)
		{
			Vector3 vector = base.inputBank.moveVector;
			Vector2 vector2 = ((!(vector == Vector3.zero)) ? Util.Vector3XZToVector2XY(vector.normalized) : Util.Vector3XZToVector2XY(base.characterDirection.forward));
			if (vector2 != Vector2.zero)
			{
				vector2.Normalize();
				idealDirection = new Vector3(vector2.x, 0f, vector2.y).normalized;
			}
		}
	}

	private Vector3 GetIdealVelocity()
	{
		return base.characterDirection.forward.normalized * base.characterBody.moveSpeed * speedMultiplier;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
		else
		{
			if (!base.isAuthority)
			{
				return;
			}
			if ((bool)base.characterBody)
			{
				base.characterBody.isSprinting = true;
			}
			if ((bool)base.skillLocator.special && base.inputBank.skill4.down)
			{
				base.skillLocator.special.ExecuteIfReady();
			}
			UpdateDirection();
			if (!inHitPause)
			{
				if ((bool)base.characterDirection)
				{
					base.characterMotor.moveDirection = idealDirection;
					base.characterDirection.moveVector = idealDirection;
					if ((bool)base.characterMotor && !base.characterMotor.disableAirControlUntilCollision)
					{
						base.characterMotor.rootMotion += GetIdealVelocity() * GetDeltaTime();
					}
				}
				if (attack.Fire())
				{
					inHitPause = true;
					hitPauseTimer = hitPauseDuration;
					AddRecoil(-0.25f * recoilAmplitude, -0.25f * recoilAmplitude, -0.25f * recoilAmplitude, 0.25f * recoilAmplitude);
				}
			}
			else
			{
				base.characterMotor.velocity = Vector3.zero;
				hitPauseTimer -= GetDeltaTime();
				if (hitPauseTimer < 0f)
				{
					inHitPause = false;
				}
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
