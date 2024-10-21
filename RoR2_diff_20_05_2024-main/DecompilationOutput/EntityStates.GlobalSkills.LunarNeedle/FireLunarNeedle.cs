using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.GlobalSkills.LunarNeedle;

public class FireLunarNeedle : BaseSkillState
{
	public static float baseDuration;

	public static float damageCoefficient;

	public static GameObject projectilePrefab;

	public static float recoilAmplitude;

	public static float spreadBloomValue;

	public static GameObject muzzleFlashEffectPrefab;

	public static string fireSound;

	public static float maxSpread;

	private float duration;

	[SerializeField]
	public string animationLayerName = "Gesture, Override";

	[SerializeField]
	public string animationStateName = "FireLunarNeedle";

	[SerializeField]
	public string playbackRateParam = "FireLunarNeedle.playbackRate";

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		if (base.isAuthority)
		{
			Ray aimRay = GetAimRay();
			aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, maxSpread, 1f, 1f);
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
		AddRecoil(-0.4f * recoilAmplitude, -0.8f * recoilAmplitude, -0.3f * recoilAmplitude, 0.3f * recoilAmplitude);
		base.characterBody.AddSpreadBloom(spreadBloomValue);
		StartAimMode();
		EffectManager.SimpleMuzzleFlash(muzzleFlashEffectPrefab, base.gameObject, "Head", transmit: false);
		Util.PlaySound(fireSound, base.gameObject);
		PlayAnimation(animationLayerName, animationStateName, playbackRateParam, duration);
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
