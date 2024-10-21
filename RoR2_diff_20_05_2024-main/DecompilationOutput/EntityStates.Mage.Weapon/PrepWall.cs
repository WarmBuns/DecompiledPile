using RoR2;
using RoR2.Projectile;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.Mage.Weapon;

public class PrepWall : BaseState
{
	public static float baseDuration;

	public static GameObject areaIndicatorPrefab;

	public static GameObject projectilePrefab;

	public static float damageCoefficient;

	public static GameObject muzzleflashEffect;

	public static GameObject goodCrosshairPrefab;

	public static GameObject badCrosshairPrefab;

	public static string prepWallSoundString;

	public static float maxDistance;

	public static string fireSoundString;

	public static float maxSlopeAngle;

	private float duration;

	private float stopwatch;

	private bool goodPlacement;

	private GameObject areaIndicatorInstance;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	private EffectManagerHelper _emh_areaIndicatorInstance;

	private static int PrepWallStateHash = Animator.StringToHash("PrepWall");

	private static int PrepWallParamHash = Animator.StringToHash("PrepWall.playbackRate");

	private static int FireWallStateHash = Animator.StringToHash("FireWall");

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		stopwatch = 0f;
		goodPlacement = false;
		areaIndicatorInstance = null;
		_emh_areaIndicatorInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		base.characterBody.SetAimTimer(duration + 2f);
		PlayAnimation("Gesture, Additive", PrepWallStateHash, PrepWallParamHash, duration);
		Util.PlaySound(prepWallSoundString, base.gameObject);
		if (!EffectManager.ShouldUsePooledEffect(areaIndicatorPrefab))
		{
			areaIndicatorInstance = Object.Instantiate(areaIndicatorPrefab);
		}
		else
		{
			_emh_areaIndicatorInstance = EffectManager.GetAndActivatePooledEffect(areaIndicatorPrefab, Vector3.zero, Quaternion.identity);
			areaIndicatorInstance = _emh_areaIndicatorInstance.gameObject;
		}
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
			Ray aimRay = GetAimRay();
			if (Physics.Raycast(CameraRigController.ModifyAimRayIfApplicable(aimRay, base.gameObject, out extraRaycastDistance), out var hitInfo, num + extraRaycastDistance, LayerIndex.world.mask))
			{
				areaIndicatorInstance.transform.position = hitInfo.point;
				areaIndicatorInstance.transform.up = hitInfo.normal;
				areaIndicatorInstance.transform.forward = -aimRay.direction;
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
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && !base.inputBank.skill3.down && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if (!outer.destroying)
		{
			if (goodPlacement)
			{
				PlayAnimation("Gesture, Additive", FireWallStateHash);
				Util.PlaySound(fireSoundString, base.gameObject);
				if ((bool)areaIndicatorInstance && base.isAuthority)
				{
					EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleLeft", transmit: true);
					EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleRight", transmit: true);
					Vector3 forward = areaIndicatorInstance.transform.forward;
					forward.y = 0f;
					forward.Normalize();
					Vector3 vector = Vector3.Cross(Vector3.up, forward);
					bool crit = Util.CheckRoll(critStat, base.characterBody.master);
					ProjectileManager.instance.FireProjectile(projectilePrefab, areaIndicatorInstance.transform.position + Vector3.up, Util.QuaternionSafeLookRotation(vector), base.gameObject, damageStat * damageCoefficient, 0f, crit);
					ProjectileManager.instance.FireProjectile(projectilePrefab, areaIndicatorInstance.transform.position + Vector3.up, Util.QuaternionSafeLookRotation(-vector), base.gameObject, damageStat * damageCoefficient, 0f, crit);
				}
			}
			else
			{
				base.skillLocator.utility.AddOneStock();
				PlayCrossfade("Gesture, Additive", "BufferEmpty", 0.2f);
			}
		}
		if (_emh_areaIndicatorInstance != null && _emh_areaIndicatorInstance.OwningPool != null)
		{
			_emh_areaIndicatorInstance.OwningPool.ReturnObject(_emh_areaIndicatorInstance);
		}
		else
		{
			EntityState.Destroy(areaIndicatorInstance.gameObject);
		}
		areaIndicatorInstance = null;
		_emh_areaIndicatorInstance = null;
		crosshairOverrideRequest?.Dispose();
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}
