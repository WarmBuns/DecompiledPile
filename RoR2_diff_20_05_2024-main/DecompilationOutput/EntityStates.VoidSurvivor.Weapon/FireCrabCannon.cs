using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.VoidSurvivor.Weapon;

public class FireCrabCannon : BaseState
{
	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public GameObject muzzleflashEffectPrefab;

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public string muzzle;

	[SerializeField]
	public int grenadeCountMax = 3;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float maxSpread;

	[SerializeField]
	public float fireDuration = 1f;

	[SerializeField]
	public float baseDuration = 2f;

	[SerializeField]
	private static float recoilAmplitude = 1f;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public string perGrenadeSoundString;

	[SerializeField]
	public float spreadBloomValue = 0.3f;

	private Transform modelTransform;

	private float duration;

	private float fireTimer;

	private int grenadeCount;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelTransform = GetModelTransform();
		StartAimMode();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	private void FireProjectile()
	{
		PlayAnimation(animationLayerName, animationStateName);
		Util.PlaySound(perGrenadeSoundString, base.gameObject);
		AddRecoil(-1f * recoilAmplitude, -2f * recoilAmplitude, -1f * recoilAmplitude, 1f * recoilAmplitude);
		base.characterBody.AddSpreadBloom(spreadBloomValue);
		if ((bool)muzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzle, transmit: false);
		}
		if (base.isAuthority)
		{
			Ray aimRay = GetAimRay();
			aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, maxSpread, 1f, 1f);
			Vector3 onUnitSphere = Random.onUnitSphere;
			Vector3.ProjectOnPlane(onUnitSphere, aimRay.direction);
			Quaternion rotation = Util.QuaternionSafeLookRotation(aimRay.direction, onUnitSphere);
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, rotation, base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		fireTimer -= GetDeltaTime();
		float num = fireDuration / attackSpeedStat / (float)grenadeCountMax;
		if (fireTimer <= 0f && grenadeCount < grenadeCountMax)
		{
			FireProjectile();
			fireTimer += num;
			grenadeCount++;
		}
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
