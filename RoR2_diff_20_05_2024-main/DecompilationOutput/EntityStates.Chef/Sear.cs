using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Chef;

public class Sear : BaseState
{
	public static GameObject flamethrowerEffectPrefab;

	public static GameObject ovenBlueEffectPrefab;

	public static GameObject initialOvenBlueEffectPrefab;

	public static GameObject boostedSearProjectilePrefab;

	public static GameObject impactEffectPrefab;

	public static GameObject tracerEffectPrefab;

	public static float maxDistance;

	public static float radius;

	public static float baseEntryDuration = 1f;

	public static float baseExitDuration = 0.5f;

	public static float baseFlamethrowerDuration = 2f;

	public static float totalDamageCoefficient = 1.2f;

	public static float procCoefficientPerTick;

	public static float tickFrequency;

	public static float force = 20f;

	public static GameObject muzzleflashEffectPrefab;

	public static float boostedProjectileDmgCoefficient = 1f;

	public static float boostedProjectileCount = 1f;

	public static float boostedProjectileTimer;

	public static float boostedProjectileInBetweenShotTime;

	public static string startAttackSoundString;

	public static string endAttackSoundString;

	public static string endLoopSoundString;

	public static string boostedStartAttackSoundString;

	public static string boostedEndAttackSoundString;

	public static string boostedEndLoopSoundString;

	public static string boostedFireBallSoundString;

	public static float ignitePercentChance;

	public static float maxSpread;

	private float tickDamageCoefficient;

	private float primaryAttackStopwatch;

	private float stopwatch;

	private float entryDuration;

	private float exitDuration;

	private float flamethrowerDuration;

	private bool flamethrowerStarted;

	private bool flamethrowerEnded;

	private ChildLocator childLocator;

	private Transform flamethrowerEffectInstance;

	private Transform muzzleTransform;

	private bool isCrit;

	private bool hasBoost;

	private float boostedDamage;

	private int boostedProjectilesFired;

	private bool allBoostProjectilesFired;

	private ChefController chefController;

	private const float flamethrowerEffectBaseDistance = 16f;

	private float flamethrowerTimestamp;

	public override void OnEnter()
	{
		base.OnEnter();
		chefController = GetComponent<ChefController>();
		if (NetworkServer.active)
		{
			base.characterBody.AddBuff(DLC2Content.Buffs.CookingSearing);
		}
		if (base.characterBody.HasBuff(DLC2Content.Buffs.Boosted))
		{
			hasBoost = true;
			base.characterBody.RemoveBuff(DLC2Content.Buffs.Boosted);
		}
		stopwatch = 0f;
		entryDuration = baseEntryDuration / attackSpeedStat;
		exitDuration = baseExitDuration / attackSpeedStat;
		flamethrowerDuration = baseFlamethrowerDuration + exitDuration;
		Transform modelTransform = GetModelTransform();
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(entryDuration + flamethrowerDuration + 1f);
		}
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
			modelTransform.GetComponent<AimAnimator>().enabled = true;
			muzzleTransform = childLocator.FindChild("MuzzleCenter");
		}
		float num = flamethrowerDuration * tickFrequency;
		if (hasBoost)
		{
			boostedDamage = 1.5f;
		}
		else
		{
			boostedDamage = 1f;
		}
		tickDamageCoefficient = totalDamageCoefficient * boostedDamage / num;
		if (base.isAuthority && (bool)base.characterBody)
		{
			isCrit = Util.CheckRoll(critStat, base.characterBody.master);
		}
		boostedProjectileTimer = boostedProjectileInBetweenShotTime;
		PlayAnimation("Gesture, Additive", "PrepSear", "PrepSear.playbackRate", entryDuration);
		PlayAnimation("Gesture, Override", "PrepSear", "PrepSear.playbackRate", entryDuration);
	}

	public override void Update()
	{
		base.Update();
		float deltaTime = Time.deltaTime;
		stopwatch += deltaTime;
		if (stopwatch < entryDuration)
		{
			return;
		}
		if (hasBoost && !allBoostProjectilesFired)
		{
			HandleBoostProjectiles(deltaTime);
			return;
		}
		if (!flamethrowerStarted && stopwatch >= entryDuration)
		{
			StartPrimaryFlameVFX();
		}
		else if (!flamethrowerEnded)
		{
			if (base.isAuthority)
			{
				HandlePrimaryAttack(deltaTime);
			}
			if (stopwatch >= entryDuration + flamethrowerDuration)
			{
				EndPrimaryFlameVFX();
			}
		}
		if (flamethrowerEnded && stopwatch >= flamethrowerDuration + entryDuration + exitDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void HandleBoostProjectiles(float deltaTime)
	{
		boostedProjectileTimer += deltaTime;
		if (!(boostedProjectileTimer < boostedProjectileInBetweenShotTime))
		{
			if ((float)boostedProjectilesFired < boostedProjectileCount)
			{
				boostedProjectileTimer = 0f;
				boostedProjectilesFired++;
				FireBoostedFireball("MuzzleCenter");
			}
			else
			{
				allBoostProjectilesFired = true;
				stopwatch = entryDuration;
			}
		}
	}

	private void FirePrimaryAttack(string muzzleString)
	{
		GetAimRay();
		if (base.isAuthority && (bool)muzzleTransform)
		{
			BulletAttack bulletAttack = new BulletAttack();
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.origin = muzzleTransform.position;
			bulletAttack.aimVector = muzzleTransform.forward;
			bulletAttack.minSpread = 0f;
			bulletAttack.maxSpread = maxSpread;
			bulletAttack.damage = tickDamageCoefficient * damageStat;
			bulletAttack.force = force;
			bulletAttack.muzzleName = muzzleString;
			bulletAttack.hitEffectPrefab = impactEffectPrefab;
			bulletAttack.isCrit = isCrit;
			bulletAttack.radius = radius;
			bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
			bulletAttack.stopperMask = LayerIndex.world.mask;
			bulletAttack.procCoefficient = procCoefficientPerTick;
			bulletAttack.maxDistance = maxDistance;
			bulletAttack.smartCollision = true;
			bulletAttack.damageType = new DamageTypeCombo(DamageType.IgniteOnHit, DamageTypeExtended.ChefSearDamage);
			bulletAttack.Fire();
		}
	}

	private void FireBoostedFireball(string muzzleString)
	{
		if (base.isAuthority && (bool)muzzleTransform)
		{
			if ((bool)muzzleflashEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: true);
			}
			Ray aimRay = GetAimRay();
			Util.PlaySound(boostedFireBallSoundString, base.gameObject);
			ProjectileManager.instance.FireProjectile(boostedSearProjectilePrefab, muzzleTransform.position, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, base.characterBody.damage * boostedProjectileDmgCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	private void StartPrimaryFlameVFX()
	{
		flamethrowerStarted = true;
		PlayAnimation("Gesture, Override", "FireSear", "FireSear.playbackRate", flamethrowerDuration);
		PlayAnimation("Gesture, Additive", "FireSear", "FireSear.playbackRate", flamethrowerDuration);
		if (hasBoost)
		{
			flamethrowerEffectInstance = Object.Instantiate(ovenBlueEffectPrefab, muzzleTransform).transform;
			Util.PlaySound(boostedStartAttackSoundString, base.gameObject);
		}
		else
		{
			flamethrowerEffectInstance = Object.Instantiate(flamethrowerEffectPrefab, muzzleTransform).transform;
			Util.PlaySound(startAttackSoundString, base.gameObject);
		}
		flamethrowerTimestamp = Time.time;
		flamethrowerEffectInstance.transform.localPosition = Vector3.zero;
		flamethrowerEffectInstance.GetComponent<ScaleParticleSystemDuration>().newDuration = flamethrowerDuration;
	}

	private void EndPrimaryFlameVFX()
	{
		flamethrowerEnded = true;
		PlayCrossfade("Gesture, Override", "ExitSear", "ExitSear.playbackRate", exitDuration, 0.1f);
		PlayCrossfade("Gesture, Additive", "ExitSear", "ExitSear.playbackRate", exitDuration, 0.1f);
		if ((bool)flamethrowerEffectInstance)
		{
			if (hasBoost)
			{
				Util.PlaySound(boostedEndLoopSoundString, base.gameObject);
				Util.PlaySound(boostedEndAttackSoundString, base.gameObject);
			}
			else
			{
				Util.PlaySound(endLoopSoundString, base.gameObject);
				Util.PlaySound(endAttackSoundString, base.gameObject);
			}
			EntityState.Destroy(flamethrowerEffectInstance.gameObject);
		}
	}

	private void HandlePrimaryAttack(float deltaTime)
	{
		primaryAttackStopwatch += deltaTime;
		if (primaryAttackStopwatch > 1f / tickFrequency)
		{
			primaryAttackStopwatch -= 1f / tickFrequency;
			FirePrimaryAttack("MuzzleCenter");
		}
	}

	public override void OnExit()
	{
		chefController.SetYesChefHeatState(newYesChefHeatState: false);
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(DLC2Content.Buffs.CookingSearing);
			if (base.characterBody.HasBuff(DLC2Content.Buffs.boostedFireEffect))
			{
				base.characterBody.RemoveBuff(DLC2Content.Buffs.boostedFireEffect);
			}
		}
		if (base.isAuthority)
		{
			chefController.ClearSkillOverrides();
		}
		PlayCrossfade("Gesture, Override", "BufferEmpty", 0.1f);
		PlayCrossfade("Gesture, Additive", "BufferEmpty", 0.1f);
		if ((bool)flamethrowerEffectInstance)
		{
			if (hasBoost)
			{
				Util.PlaySound(boostedEndLoopSoundString, base.gameObject);
				Util.PlaySound(boostedEndAttackSoundString, base.gameObject);
			}
			else
			{
				Util.PlaySound(endLoopSoundString, base.gameObject);
				Util.PlaySound(endAttackSoundString, base.gameObject);
			}
			EntityState.Destroy(flamethrowerEffectInstance.gameObject);
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}
