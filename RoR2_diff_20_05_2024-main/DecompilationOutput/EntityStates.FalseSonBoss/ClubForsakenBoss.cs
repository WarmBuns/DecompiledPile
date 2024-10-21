using RoR2;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class ClubForsakenBoss : BaseSkillState
{
	[SerializeField]
	public float baseChargeDuration = 1f;

	public static float minChargeForChargedAttack;

	public static GameObject chargeVfxPrefab;

	public static string chargeVfxChildLocatorName;

	public static GameObject crosshairOverridePrefab;

	public static float walkSpeedCoefficient;

	public static string startChargeLoopSFXString;

	public static string endChargeLoopSFXString;

	public static string enterSFXString;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	private Transform chargeVfxInstanceTransform;

	private int gauntlet;

	private uint soundID;

	private bool chargeAnimPlayed;

	protected float chargeDuration { get; private set; }

	protected float charge { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		chargeDuration = baseChargeDuration;
		Util.PlaySound(enterSFXString, base.gameObject);
		soundID = Util.PlaySound(startChargeLoopSFXString, base.gameObject);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	public override void OnExit()
	{
		if ((bool)chargeVfxInstanceTransform)
		{
			EntityState.Destroy(chargeVfxInstanceTransform.gameObject);
			PlayAnimation("Gesture, Additive", "Empty");
			PlayAnimation("Gesture, Override", "Empty");
			crosshairOverrideRequest?.Dispose();
			chargeVfxInstanceTransform = null;
		}
		base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
		Util.PlaySound(endChargeLoopSFXString, base.gameObject);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		charge = Mathf.Clamp01(base.fixedAge / chargeDuration);
		AkSoundEngine.SetRTPCValueByPlayingID("loaderShift_chargeAmount", charge * 100f, soundID);
		base.characterBody.SetSpreadBloom(charge);
		base.characterBody.SetAimTimer(3f);
		if (charge >= minChargeForChargedAttack && !chargeVfxInstanceTransform && (bool)chargeVfxPrefab)
		{
			if ((bool)crosshairOverridePrefab && crosshairOverrideRequest == null)
			{
				crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
			}
			Transform transform = FindModelChild(chargeVfxChildLocatorName);
			if ((bool)transform)
			{
				chargeVfxInstanceTransform = Object.Instantiate(chargeVfxPrefab, transform).transform;
				ScaleParticleSystemDuration component = chargeVfxInstanceTransform.GetComponent<ScaleParticleSystemDuration>();
				if ((bool)component)
				{
					component.newDuration = (1f - minChargeForChargedAttack) * chargeDuration;
				}
			}
			if (!chargeAnimPlayed)
			{
				PlayCrossfade("Gesture, Additive", "ChargeSwingIntro", "ChargeSwingIntro.playbackRate", 0.2f * chargeDuration, 0.1f);
				PlayCrossfade("Gesture, Override", "ChargeSwingIntro", "ChargeSwingIntro.playbackRate", 0.2f * chargeDuration, 0.1f);
				chargeAnimPlayed = true;
			}
		}
		if ((bool)chargeVfxInstanceTransform)
		{
			base.characterMotor.walkSpeedPenaltyCoefficient = walkSpeedCoefficient;
		}
		if (base.isAuthority)
		{
			AuthorityFixedUpdate();
		}
	}

	public override void Update()
	{
		base.Update();
		Mathf.Clamp01(base.age / chargeDuration);
	}

	private void AuthorityFixedUpdate()
	{
		ShouldKeepChargingAuthority();
	}

	protected virtual bool ShouldKeepChargingAuthority()
	{
		return IsKeyDownAuthority();
	}
}
