using RoR2;
using UnityEngine;

namespace EntityStates.Captain.Weapon;

public class ChargeCaptainShotgun : BaseState
{
	public static float baseMinChargeDuration;

	public static float baseChargeDuration;

	public static string muzzleName;

	public static GameObject chargeupVfxPrefab;

	public static GameObject holdChargeVfxPrefab;

	public static string enterSoundString;

	public static string playChargeSoundString;

	public static string stopChargeSoundString;

	private float minChargeDuration;

	private float chargeDuration;

	private bool released;

	private GameObject chargeupVfxGameObject;

	private GameObject holdChargeVfxGameObject;

	private Transform muzzleTransform;

	private uint enterSoundID;

	public override void OnEnter()
	{
		base.OnEnter();
		minChargeDuration = baseMinChargeDuration / attackSpeedStat;
		chargeDuration = baseChargeDuration / attackSpeedStat;
		PlayCrossfade("Gesture, Override", "ChargeCaptainShotgun", "ChargeCaptainShotgun.playbackRate", chargeDuration, 0.1f);
		PlayCrossfade("Gesture, Additive", "ChargeCaptainShotgun", "ChargeCaptainShotgun.playbackRate", chargeDuration, 0.1f);
		muzzleTransform = FindModelChild(muzzleName);
		if ((bool)muzzleTransform)
		{
			chargeupVfxGameObject = Object.Instantiate(chargeupVfxPrefab, muzzleTransform);
			chargeupVfxGameObject.GetComponent<ScaleParticleSystemDuration>().newDuration = chargeDuration;
		}
		enterSoundID = Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, attackSpeedStat);
		Util.PlaySound(playChargeSoundString, base.gameObject);
	}

	public override void OnExit()
	{
		if ((bool)chargeupVfxGameObject)
		{
			EntityState.Destroy(chargeupVfxGameObject);
			chargeupVfxGameObject = null;
		}
		if ((bool)holdChargeVfxGameObject)
		{
			EntityState.Destroy(holdChargeVfxGameObject);
			holdChargeVfxGameObject = null;
		}
		AkSoundEngine.StopPlayingID(enterSoundID);
		Util.PlaySound(stopChargeSoundString, base.gameObject);
		base.OnExit();
	}

	public override void Update()
	{
		base.Update();
		base.characterBody.SetSpreadBloom(base.age / chargeDuration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		base.characterBody.SetAimTimer(1f);
		Mathf.Clamp01(base.fixedAge / chargeDuration);
		if (base.fixedAge >= chargeDuration)
		{
			if ((bool)chargeupVfxGameObject)
			{
				EntityState.Destroy(chargeupVfxGameObject);
				chargeupVfxGameObject = null;
			}
			if (!holdChargeVfxGameObject && (bool)muzzleTransform)
			{
				holdChargeVfxGameObject = Object.Instantiate(holdChargeVfxPrefab, muzzleTransform);
			}
		}
		if (base.isAuthority)
		{
			if (!released && (!base.inputBank || !base.inputBank.skill1.down))
			{
				released = true;
			}
			if (released)
			{
				outer.SetNextState(new FireCaptainShotgun());
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
