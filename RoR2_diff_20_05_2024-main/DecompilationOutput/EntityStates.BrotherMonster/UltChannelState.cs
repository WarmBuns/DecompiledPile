using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;

namespace EntityStates.BrotherMonster;

public class UltChannelState : BaseState
{
	public static GameObject waveProjectileLeftPrefab;

	public static GameObject waveProjectileRightPrefab;

	public static int waveProjectileCount;

	public static float waveProjectileDamageCoefficient;

	public static float waveProjectileForce;

	public static int totalWaves;

	public static float maxDuration;

	public static GameObject channelBeginMuzzleflashEffectPrefab;

	public static GameObject channelEffectPrefab;

	public static string enterSoundString;

	public static string exitSoundString;

	private GameObject channelEffectInstance;

	public static SkillDef replacementSkillDef;

	private int wavesFired;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(enterSoundString, base.gameObject);
		Transform transform = FindModelChild("MuzzleUlt");
		if ((bool)transform && (bool)channelEffectPrefab)
		{
			channelEffectInstance = Object.Instantiate(channelEffectPrefab, transform.position, Quaternion.identity, transform);
		}
		if ((bool)channelBeginMuzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(channelBeginMuzzleflashEffectPrefab, base.gameObject, "MuzzleUlt", transmit: false);
		}
	}

	private void FireWave()
	{
		wavesFired++;
		float num = 360f / (float)waveProjectileCount;
		Vector3 normalized = Vector3.ProjectOnPlane(Random.onUnitSphere, Vector3.up).normalized;
		Vector3 footPosition = base.characterBody.footPosition;
		GameObject prefab = waveProjectileLeftPrefab;
		if (Random.value <= 0.5f)
		{
			prefab = waveProjectileRightPrefab;
		}
		for (int i = 0; i < waveProjectileCount; i++)
		{
			Vector3 forward = Quaternion.AngleAxis(num * (float)i, Vector3.up) * normalized;
			ProjectileManager.instance.FireProjectile(prefab, footPosition, Util.QuaternionSafeLookRotation(forward), base.gameObject, base.characterBody.damage * waveProjectileDamageCoefficient, waveProjectileForce, Util.CheckRoll(base.characterBody.crit, base.characterBody.master));
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			if (Mathf.CeilToInt(base.fixedAge / maxDuration * (float)totalWaves) > wavesFired)
			{
				FireWave();
			}
			if (base.fixedAge > maxDuration)
			{
				outer.SetNextState(new UltExitState());
			}
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(exitSoundString, base.gameObject);
		if ((bool)channelEffectInstance)
		{
			EntityState.Destroy(channelEffectInstance);
		}
		base.OnExit();
	}
}
