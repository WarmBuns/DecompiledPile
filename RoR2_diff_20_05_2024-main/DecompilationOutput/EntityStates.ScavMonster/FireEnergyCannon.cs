using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.ScavMonster;

public class FireEnergyCannon : EnergyCannonState
{
	public static float baseDuration;

	public static float baseRefireDuration;

	public static string sound;

	public static GameObject effectPrefab;

	public static GameObject projectilePrefab;

	public static float damageCoefficient;

	public static float force;

	public static float minSpread;

	public static float maxSpread;

	public static float recoilAmplitude = 1f;

	public static float projectilePitchBonus;

	public static float projectileYawBonusPerRefire;

	public static int projectileCount;

	public static int maxRefireCount;

	public int currentRefire;

	private float duration;

	private float refireDuration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		refireDuration = baseRefireDuration / attackSpeedStat;
		Util.PlayAttackSpeedSound(sound, base.gameObject, attackSpeedStat);
		PlayCrossfade("Body", "FireEnergyCannon", "FireEnergyCannon.playbackRate", duration, 0.1f);
		AddRecoil(-2f * recoilAmplitude, -3f * recoilAmplitude, -1f * recoilAmplitude, 1f * recoilAmplitude);
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, EnergyCannonState.muzzleName, transmit: false);
		}
		if (base.isAuthority)
		{
			float num = ((currentRefire % 2 == 0) ? 1 : (-1));
			float num2 = Mathf.Ceil((float)currentRefire / 2f) * projectileYawBonusPerRefire;
			for (int i = 0; i < projectileCount; i++)
			{
				Ray aimRay = GetAimRay();
				aimRay.direction = Util.ApplySpread(aimRay.direction, minSpread, maxSpread, 1f, 1f, num * num2, projectilePitchBonus);
				ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= refireDuration && currentRefire + 1 < maxRefireCount && base.isAuthority)
		{
			FireEnergyCannon fireEnergyCannon = new FireEnergyCannon();
			fireEnergyCannon.currentRefire = currentRefire + 1;
			outer.SetNextState(fireEnergyCannon);
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}
