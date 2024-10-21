using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Seeker;

public class SoulSpiral : BaseState
{
	public static string prepOrbSoundString;

	public static float duration;

	public static float dmgCoefficient;

	public static GameObject projectilePrefab;

	public static GameObject muzzleFXPrefab;

	public static string muzzleChildName;

	public static float projectileForce;

	public static float smallHopVelocity;

	public bool firedOrb;

	private EffectData effectData;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.modelLocator)
		{
			ChildLocator component = base.modelLocator.modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				int childIndex = component.FindChildIndex(muzzleChildName);
				effectData = new EffectData();
				effectData.SetChildLocatorTransformReference(base.characterBody.gameObject, childIndex);
			}
		}
		PlayAnimation("Gesture, Additive", "SoulSpiralStart", "PrepUnseenHand.playbackRate", duration);
		PlayAnimation("Gesture, Override", "SoulSpiralStart", "PrepUnseenHand.playbackRate", duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!firedOrb && base.fixedAge > duration && base.isAuthority)
		{
			FireSpiral();
			outer.SetNextStateToMain();
		}
	}

	private void FireSpiral()
	{
		firedOrb = true;
		Util.PlaySound(prepOrbSoundString, base.gameObject);
		effectData.origin = base.characterBody.corePosition;
		effectData.rotation = Quaternion.identity;
		EffectManager.SpawnEffect(muzzleFXPrefab, effectData, transmit: false);
		if (base.isAuthority)
		{
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.damage = damageStat * dmgCoefficient;
			fireProjectileInfo.force = projectileForce;
			fireProjectileInfo.position = base.characterBody.corePosition;
			fireProjectileInfo.crit = Util.CheckRoll(critStat, base.characterBody.master);
			FireProjectileInfo projectileInfo = fireProjectileInfo;
			base.characterBody.GetComponent<SeekerController>().FireSoulSpiral(projectileInfo);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if (!firedOrb && !ObjectIsBeingDestroyed())
		{
			FireSpiral();
		}
		if ((bool)base.characterMotor && !base.isGrounded)
		{
			base.characterMotor.velocity = new Vector3(base.characterMotor.velocity.x, Mathf.Max(base.characterMotor.velocity.y, smallHopVelocity), base.characterMotor.velocity.z);
		}
	}

	private bool ObjectIsBeingDestroyed()
	{
		GameObject gameObject = base.characterBody.gameObject;
		if (!(gameObject == null))
		{
			if (!gameObject.activeSelf)
			{
				return gameObject.activeInHierarchy;
			}
			return false;
		}
		return true;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Any;
	}
}
