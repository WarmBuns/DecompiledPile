using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.BrotherMonster.Weapon;

public class FireLunarShards : BaseSkillState
{
	public static float baseDuration;

	[SerializeField]
	public float damageCoefficient;

	public static GameObject projectilePrefab;

	public static float recoilAmplitude;

	public static float spreadBloomValue;

	public static string muzzleString;

	public static GameObject muzzleFlashEffectPrefab;

	public static string fireSound;

	[SerializeField]
	public float maxSpread;

	[SerializeField]
	public float spreadYawScale;

	[SerializeField]
	public float spreadPitchScale;

	private float duration;

	private static int FireLunarShardsStateHash = Animator.StringToHash("FireLunarShards");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		if (base.isAuthority)
		{
			Ray aimRay = GetAimRay();
			Transform transform = FindModelChild(muzzleString);
			if ((bool)transform)
			{
				aimRay.origin = transform.position;
			}
			aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, maxSpread, spreadYawScale, spreadPitchScale);
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.position = aimRay.origin;
			fireProjectileInfo.rotation = Quaternion.LookRotation(aimRay.direction);
			fireProjectileInfo.crit = base.characterBody.RollCrit();
			fireProjectileInfo.damage = base.characterBody.damage * damageCoefficient;
			fireProjectileInfo.damageColorIndex = DamageColorIndex.Default;
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.procChainMask = default(ProcChainMask);
			fireProjectileInfo.force = 0f;
			fireProjectileInfo.useFuseOverride = false;
			fireProjectileInfo.useSpeedOverride = false;
			fireProjectileInfo.target = null;
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo);
		}
		PlayAnimation("Gesture, Additive", FireLunarShardsStateHash);
		PlayAnimation("Gesture, Override", FireLunarShardsStateHash);
		AddRecoil(-0.4f * recoilAmplitude, -0.8f * recoilAmplitude, -0.3f * recoilAmplitude, 0.3f * recoilAmplitude);
		base.characterBody.AddSpreadBloom(spreadBloomValue);
		EffectManager.SimpleMuzzleFlash(muzzleFlashEffectPrefab, base.gameObject, muzzleString, transmit: false);
		Util.PlaySound(fireSound, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
