using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Treebot.Weapon;

public class AimMortar2 : AimThrowableBase
{
	[SerializeField]
	public float healthCostFraction;

	public static string muzzleName;

	[SerializeField]
	public GameObject muzzleFlashPrefab;

	[SerializeField]
	public string fireSound;

	[SerializeField]
	public string enterSound;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayCrossfade("Gesture, Additive", "PrepBomb", 0.1f);
		Util.PlaySound(enterSound, base.gameObject);
	}

	protected override void OnProjectileFiredLocal()
	{
		if (NetworkServer.active && (bool)base.healthComponent && healthCostFraction >= Mathf.Epsilon)
		{
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.damage = base.healthComponent.combinedHealth * healthCostFraction;
			damageInfo.position = base.characterBody.corePosition;
			damageInfo.force = Vector3.zero;
			damageInfo.damageColorIndex = DamageColorIndex.Default;
			damageInfo.crit = false;
			damageInfo.attacker = null;
			damageInfo.inflictor = null;
			damageInfo.damageType = DamageType.NonLethal;
			damageInfo.procCoefficient = 0f;
			damageInfo.procChainMask = default(ProcChainMask);
			base.healthComponent.TakeDamage(damageInfo);
		}
		EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, muzzleName, transmit: false);
		Util.PlaySound(fireSound, base.gameObject);
		PlayCrossfade("Gesture, Additive", "FireBomb", 0.1f);
	}

	protected override bool KeyIsDown()
	{
		return base.inputBank.skill2.down;
	}

	protected override void ModifyProjectile(ref FireProjectileInfo fireProjectileInfo)
	{
		base.ModifyProjectile(ref fireProjectileInfo);
		fireProjectileInfo.position = currentTrajectoryInfo.hitPoint;
		fireProjectileInfo.rotation = Quaternion.identity;
		fireProjectileInfo.speedOverride = 0f;
	}
}
