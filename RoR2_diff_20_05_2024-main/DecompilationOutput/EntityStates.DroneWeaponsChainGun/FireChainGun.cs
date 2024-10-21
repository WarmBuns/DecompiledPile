using RoR2;
using RoR2.Orbs;
using UnityEngine;

namespace EntityStates.DroneWeaponsChainGun;

public class FireChainGun : BaseDroneWeaponChainGunState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public GameObject orbEffectObject;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float orbSpeed;

	[SerializeField]
	public int shotCount;

	[SerializeField]
	public float procCoefficient;

	[SerializeField]
	public int additionalBounces;

	[SerializeField]
	public float bounceRange;

	[SerializeField]
	public float damageCoefficientPerBounce;

	[SerializeField]
	public int targetsToFindPerBounce;

	[SerializeField]
	public bool canBounceOnSameTarget;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public GameObject muzzleFlashPrefab;

	[SerializeField]
	public string fireSoundString;

	private HurtBox targetHurtBox;

	private float duration;

	private int stepIndex;

	public FireChainGun()
	{
	}

	public FireChainGun(HurtBox targetHurtBox)
	{
		this.targetHurtBox = targetHurtBox;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		Transform transform = FindChild(muzzleName);
		if (!transform)
		{
			transform = body.coreTransform;
		}
		if (base.isAuthority)
		{
			duration = baseDuration / body.attackSpeed;
			ChainGunOrb chainGunOrb = new ChainGunOrb(orbEffectObject);
			chainGunOrb.damageValue = body.damage * damageCoefficient;
			chainGunOrb.isCrit = Util.CheckRoll(body.crit, body.master);
			chainGunOrb.teamIndex = TeamComponent.GetObjectTeam(body.gameObject);
			chainGunOrb.attacker = body.gameObject;
			chainGunOrb.procCoefficient = procCoefficient;
			chainGunOrb.procChainMask = default(ProcChainMask);
			chainGunOrb.origin = transform.position;
			chainGunOrb.target = targetHurtBox;
			chainGunOrb.speed = orbSpeed;
			chainGunOrb.bouncesRemaining = additionalBounces;
			chainGunOrb.bounceRange = bounceRange;
			chainGunOrb.damageCoefficientPerBounce = damageCoefficientPerBounce;
			chainGunOrb.targetsToFindPerBounce = targetsToFindPerBounce;
			chainGunOrb.canBounceOnSameTarget = canBounceOnSameTarget;
			chainGunOrb.damageColorIndex = DamageColorIndex.Item;
			OrbManager.instance.AddOrb(chainGunOrb);
		}
		if ((bool)transform)
		{
			EffectData effectData = new EffectData
			{
				origin = transform.position
			};
			EffectManager.SpawnEffect(muzzleFlashPrefab, effectData, transmit: true);
		}
		Util.PlaySound(fireSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge > duration)
		{
			BaseDroneWeaponChainGunState baseDroneWeaponChainGunState;
			if (stepIndex < shotCount)
			{
				baseDroneWeaponChainGunState = new FireChainGun(targetHurtBox);
				(baseDroneWeaponChainGunState as FireChainGun).stepIndex = stepIndex + 1;
			}
			else
			{
				baseDroneWeaponChainGunState = new AimChainGun();
			}
			baseDroneWeaponChainGunState.PassDisplayLinks(gunChildLocators, gunAnimators);
			outer.SetNextState(baseDroneWeaponChainGunState);
		}
	}
}
