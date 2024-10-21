using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Mage.Weapon;

public class FireRoller : BaseState
{
	public static GameObject fireProjectilePrefab;

	public static GameObject iceProjectilePrefab;

	public static GameObject lightningProjectilePrefab;

	public static GameObject fireMuzzleflashEffectPrefab;

	public static GameObject iceMuzzleflashEffectPrefab;

	public static GameObject lightningMuzzleflashEffectPrefab;

	public static GameObject fireAreaIndicatorPrefab;

	public static GameObject iceAreaIndicatorPrefab;

	public static GameObject lightningAreaIndicatorPrefab;

	public static string fireAttackSoundString;

	public static string iceAttackSoundString;

	public static string lightningAttackSoundString;

	public static float targetProjectileSpeed;

	public static float baseEntryDuration = 2f;

	public static float baseDuration = 2f;

	public static float baseExitDuration = 2f;

	public static float fireDamageCoefficient;

	public static float iceDamageCoefficient;

	public static float lightningDamageCoefficient;

	private float stopwatch;

	private float fireDuration;

	private float entryDuration;

	private float exitDuration;

	private bool hasFiredRoller;

	private bool hasBegunExit;

	private GameObject areaIndicatorInstance;

	private string muzzleString;

	private Transform muzzleTransform;

	private Animator animator;

	private ChildLocator childLocator;

	private GameObject areaIndicatorPrefab;

	private float damageCoefficient = 1.2f;

	private string attackString;

	private GameObject projectilePrefab;

	private GameObject muzzleflashEffectPrefab;

	private EffectManagerHelper _emh_areaIndicatorInstance;

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	private static int EnterRollerStateHash = Animator.StringToHash("EnterRoller");

	private static int EnterRollerParamHash = Animator.StringToHash("EnterRoller.playbackRate");

	private static int FireRollerStateHash = Animator.StringToHash("FireRoller");

	private static int FireRollerParamHash = Animator.StringToHash("FireRoller.playbackRate");

	private static int ExitRollerStateHash = Animator.StringToHash("ExitRoller");

	private static int ExitRollerParamHash = Animator.StringToHash("ExitRoller.playbackRate");

	public override void Reset()
	{
		base.Reset();
		fireDuration = 0f;
		entryDuration = 0f;
		exitDuration = 0f;
		hasFiredRoller = false;
		hasBegunExit = false;
		areaIndicatorInstance = null;
		muzzleString = string.Empty;
		muzzleTransform = null;
		animator = null;
		childLocator = null;
		areaIndicatorPrefab = null;
		damageCoefficient = 1.2f;
		attackString = string.Empty;
		projectilePrefab = null;
		muzzleflashEffectPrefab = null;
		_emh_areaIndicatorInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		InitElement(MageElement.Ice);
		stopwatch = 0f;
		entryDuration = baseEntryDuration / attackSpeedStat;
		fireDuration = baseDuration / attackSpeedStat;
		exitDuration = baseExitDuration / attackSpeedStat;
		Util.PlaySound(attackString, base.gameObject);
		base.characterBody.SetAimTimer(fireDuration + entryDuration + exitDuration + 2f);
		animator = GetModelAnimator();
		if ((bool)animator)
		{
			childLocator = animator.GetComponent<ChildLocator>();
		}
		muzzleString = "MuzzleRight";
		if ((bool)childLocator)
		{
			muzzleTransform = childLocator.FindChild(muzzleString);
		}
		PlayAnimation("Gesture Left, Additive", EmptyStateHash);
		PlayAnimation("Gesture Right, Additive", EmptyStateHash);
		PlayAnimation("Gesture, Additive", EnterRollerStateHash, EnterRollerParamHash, entryDuration);
		if ((bool)areaIndicatorPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(areaIndicatorPrefab))
			{
				areaIndicatorInstance = Object.Instantiate(areaIndicatorPrefab);
				return;
			}
			_emh_areaIndicatorInstance = EffectManager.GetAndActivatePooledEffect(areaIndicatorPrefab, Vector3.zero, Quaternion.identity);
			areaIndicatorInstance = _emh_areaIndicatorInstance.gameObject;
		}
	}

	private void UpdateAreaIndicator()
	{
		if ((bool)areaIndicatorInstance)
		{
			float maxDistance = 1000f;
			if (Physics.Raycast(GetAimRay(), out var hitInfo, maxDistance, LayerIndex.world.mask))
			{
				areaIndicatorInstance.transform.position = hitInfo.point;
				areaIndicatorInstance.transform.rotation = Util.QuaternionSafeLookRotation(base.transform.position - areaIndicatorInstance.transform.position, hitInfo.normal);
			}
		}
	}

	public override void Update()
	{
		base.Update();
		UpdateAreaIndicator();
	}

	protected void DestroyAreaIndicator()
	{
		if (areaIndicatorInstance != null)
		{
			if (_emh_areaIndicatorInstance != null && _emh_areaIndicatorInstance.OwningPool != null)
			{
				_emh_areaIndicatorInstance.OwningPool.ReturnObject(_emh_areaIndicatorInstance);
			}
			else
			{
				EntityState.Destroy(areaIndicatorInstance);
			}
			areaIndicatorInstance = null;
			_emh_areaIndicatorInstance = null;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		DestroyAreaIndicator();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= entryDuration && !hasFiredRoller)
		{
			PlayAnimation("Gesture, Additive", FireRollerStateHash, FireRollerParamHash, fireDuration);
			FireRollerProjectile();
			DestroyAreaIndicator();
		}
		if (stopwatch >= entryDuration + fireDuration && !hasBegunExit)
		{
			hasBegunExit = true;
			PlayAnimation("Gesture, Additive", ExitRollerStateHash, ExitRollerParamHash, exitDuration);
		}
		if (stopwatch >= entryDuration + fireDuration + exitDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void FireRollerProjectile()
	{
		hasFiredRoller = true;
		if ((bool)muzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: false);
		}
		if (!base.isAuthority || !(projectilePrefab != null))
		{
			return;
		}
		float maxDistance = 1000f;
		Ray aimRay = GetAimRay();
		Vector3 forward = aimRay.direction;
		Vector3 vector = aimRay.origin;
		float magnitude = targetProjectileSpeed;
		if ((bool)muzzleTransform)
		{
			vector = muzzleTransform.position;
			if (Physics.Raycast(aimRay, out var hitInfo, maxDistance, LayerIndex.world.mask))
			{
				float num = magnitude;
				Vector3 vector2 = hitInfo.point - vector;
				Vector2 vector3 = new Vector2(vector2.x, vector2.z);
				float magnitude2 = vector3.magnitude;
				float y = Trajectory.CalculateInitialYSpeed(magnitude2 / num, vector2.y);
				Vector3 vector4 = new Vector3(vector3.x / magnitude2 * num, y, vector3.y / magnitude2 * num);
				magnitude = vector4.magnitude;
				forward = vector4 / magnitude;
			}
		}
		ProjectileManager.instance.FireProjectile(projectilePrefab, vector, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master), DamageColorIndex.Default, null, magnitude);
	}

	private void InitElement(MageElement defaultElement)
	{
		MageCalibrationController component = GetComponent<MageCalibrationController>();
		if ((bool)component)
		{
			MageElement activeCalibrationElement = component.GetActiveCalibrationElement();
			if (activeCalibrationElement != 0)
			{
				defaultElement = activeCalibrationElement;
			}
		}
		switch (defaultElement)
		{
		case MageElement.Fire:
			damageCoefficient = fireDamageCoefficient;
			attackString = fireAttackSoundString;
			projectilePrefab = fireProjectilePrefab;
			muzzleflashEffectPrefab = fireMuzzleflashEffectPrefab;
			areaIndicatorPrefab = fireAreaIndicatorPrefab;
			break;
		case MageElement.Ice:
			damageCoefficient = iceDamageCoefficient;
			attackString = iceAttackSoundString;
			projectilePrefab = iceProjectilePrefab;
			muzzleflashEffectPrefab = iceMuzzleflashEffectPrefab;
			areaIndicatorPrefab = iceAreaIndicatorPrefab;
			break;
		case MageElement.Lightning:
			damageCoefficient = lightningDamageCoefficient;
			attackString = lightningAttackSoundString;
			projectilePrefab = lightningProjectilePrefab;
			muzzleflashEffectPrefab = lightningMuzzleflashEffectPrefab;
			areaIndicatorPrefab = lightningAreaIndicatorPrefab;
			break;
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
