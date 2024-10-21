using RoR2;
using RoR2.Projectile;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.Seeker;

public class UnseenHand : BaseState
{
	public static float baseDuration;

	public static GameObject areaIndicatorPrefab;

	public static GameObject projectilePrefab;

	public static GameObject muzzleflashEffect;

	public static GameObject goodCrosshairPrefab;

	public static GameObject badCrosshairPrefab;

	public static string prepFistSoundString;

	public static string endFistSoundString;

	public static string startTargetingLoopSoundString;

	public static string stopTargetingLoopSoundString;

	public static float maxDistance;

	public static string fireSoundString;

	public static float maxSlopeAngle;

	public static GameObject initialEffect;

	public static GameObject muzzleFlashEffect;

	public static GameObject fistProjectilePrefab;

	public static float fistDamageCoefficient;

	public static float fistForce;

	public static float fuseOverride;

	public static int abilityAimType;

	public static float cameraTransitionOutTime;

	public static float smallHopVelocity;

	private float duration;

	private float stopwatch;

	private bool goodPlacement;

	private GameObject areaIndicatorInstance;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	protected CameraTargetParams.AimRequest aimRequest;

	private bool cameraStarted;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		base.characterBody.SetAimTimer(duration + 2f);
		PlayAnimation("Gesture, Additive", "UnseenHandPrep", "PrepUnseenHand.playbackRate", duration);
		PlayAnimation("Gesture, Override", "UnseenHandPrep", "PrepUnseenHand.playbackRate", duration);
		Util.PlaySound(prepFistSoundString, base.gameObject);
		Util.PlaySound(startTargetingLoopSoundString, base.gameObject);
		areaIndicatorInstance = Object.Instantiate(areaIndicatorPrefab);
		UpdateAreaIndicator();
	}

	private void UpdateAreaIndicator()
	{
		bool flag = goodPlacement;
		goodPlacement = false;
		areaIndicatorInstance.SetActive(value: true);
		if ((bool)areaIndicatorInstance)
		{
			float num = maxDistance;
			float extraRaycastDistance = 0f;
			if (Physics.Raycast(CameraRigController.ModifyAimRayIfApplicable(GetAimRay(), base.gameObject, out extraRaycastDistance), out var hitInfo, num + extraRaycastDistance, LayerIndex.world.mask))
			{
				areaIndicatorInstance.transform.position = hitInfo.point;
				goodPlacement = Vector3.Angle(Vector3.up, hitInfo.normal) < maxSlopeAngle;
			}
			if (flag != goodPlacement || crosshairOverrideRequest == null)
			{
				crosshairOverrideRequest?.Dispose();
				GameObject crosshairPrefab = (goodPlacement ? goodCrosshairPrefab : badCrosshairPrefab);
				crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairPrefab, CrosshairUtils.OverridePriority.Skill);
			}
		}
		areaIndicatorInstance.SetActive(goodPlacement);
	}

	public override void Update()
	{
		base.Update();
		UpdateAreaIndicator();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!cameraStarted && base.fixedAge > 0.1f)
		{
			if ((bool)base.cameraTargetParams)
			{
				aimRequest = base.cameraTargetParams.RequestAimType((CameraTargetParams.AimType)abilityAimType);
			}
			cameraStarted = true;
		}
		if (base.inputBank.skill2.down || !base.isAuthority)
		{
			return;
		}
		if (goodPlacement)
		{
			PlayAnimation("Gesture, Additive", "UnseenHandFinish");
			PlayAnimation("Gesture, Override", "UnseenHandFinish");
			Util.PlaySound(fireSoundString, base.gameObject);
			if ((bool)areaIndicatorInstance)
			{
				EffectManager.SpawnEffect(muzzleFlashEffect, new EffectData
				{
					origin = areaIndicatorInstance.transform.position
				}, transmit: true);
				if (base.isAuthority)
				{
					EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleLeft", transmit: true);
					EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleRight", transmit: true);
					Vector3 forward = areaIndicatorInstance.transform.forward;
					forward.y = 0f;
					forward.Normalize();
					Vector3.Cross(Vector3.up, forward);
					Util.CheckRoll(critStat, base.characterBody.master);
					FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
					fireProjectileInfo.projectilePrefab = fistProjectilePrefab;
					fireProjectileInfo.position = areaIndicatorInstance.transform.position;
					fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Vector3.up);
					fireProjectileInfo.owner = base.gameObject;
					fireProjectileInfo.damage = damageStat * fistDamageCoefficient;
					fireProjectileInfo.damageTypeOverride = DamageType.Stun1s;
					fireProjectileInfo.force = fistForce;
					fireProjectileInfo.crit = base.characterBody.RollCrit();
					fireProjectileInfo.fuseOverride = fuseOverride;
					ProjectileManager.instance.FireProjectile(fireProjectileInfo);
				}
			}
		}
		else
		{
			base.skillLocator.secondary.AddOneStock();
			PlayCrossfade("Gesture, Additive", "BufferEmpty", 0.2f);
			PlayCrossfade("Gesture, Override", "BufferEmpty", 0.2f);
		}
		outer.SetNextStateToMain();
	}

	public override void OnExit()
	{
		if (goodPlacement)
		{
			if (!base.isGrounded)
			{
				base.characterMotor.velocity.y = Mathf.Max(base.characterMotor.velocity.y, smallHopVelocity);
			}
			if (base.characterBody.isServer)
			{
				CharacterBody obj = base.characterBody;
				if ((object)obj != null && obj.inventory.GetItemCount(DLC2Content.Items.IncreasePrimaryDamage) > 0)
				{
					base.characterBody.AddIncreasePrimaryDamageStack();
				}
			}
		}
		Util.PlaySound(endFistSoundString, base.gameObject);
		Util.PlaySound(stopTargetingLoopSoundString, base.gameObject);
		EntityState.Destroy(areaIndicatorInstance.gameObject);
		crosshairOverrideRequest?.Dispose();
		aimRequest?.Dispose();
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}
