using RoR2;
using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class SwatAwayPlayersWindup : BaseSkillState
{
	[SerializeField]
	public float baseChargeDuration = 1f;

	[SerializeField]
	public float chargeThreshold = 3f;

	public static float minChargeForChargedAttack;

	public static GameObject chargeVfxPrefab;

	public static string chargeVfxChildLocatorName;

	public static GameObject crosshairOverridePrefab;

	public static float walkSpeedCoefficient;

	public static string startChargeLoopSFXString;

	public static string endChargeLoopSFXString;

	public static string enterSFXString;

	private int gauntlet;

	private uint soundID;

	private Transform chargeVfxInstanceTransform;

	private bool chargeAnimPlayed;

	private bool stateAborted;

	protected float chargeDuration { get; private set; }

	protected float charge { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		chargeDuration = 0.5f;
		Util.PlaySound(enterSFXString, base.gameObject);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}

	public override void OnExit()
	{
		if (!stateAborted)
		{
			if ((bool)chargeVfxInstanceTransform)
			{
				EntityState.Destroy(chargeVfxInstanceTransform.gameObject);
				PlayAnimation("Gesture, Additive", "Empty");
				PlayAnimation("Gesture, Override", "Empty");
				chargeVfxInstanceTransform = null;
			}
			base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
			Util.PlaySound(endChargeLoopSFXString, base.gameObject);
			base.OnExit();
		}
	}

	public override void FixedUpdate()
	{
		if (stateAborted)
		{
			return;
		}
		base.FixedUpdate();
		charge += Time.deltaTime;
		base.characterBody.SetSpreadBloom(charge);
		base.characterBody.SetAimTimer(3f);
		if (charge <= 3f && !chargeVfxInstanceTransform && (bool)chargeVfxPrefab)
		{
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
				PlayCrossfade("Gesture, Additive", "ChargeSwingIntro", "ChargeSwingIntro.playbackRate", chargeDuration, 0.1f);
				PlayCrossfade("Gesture, Override", "ChargeSwingIntro", "ChargeSwingIntro.playbackRate", chargeDuration, 0.1f);
				chargeAnimPlayed = true;
			}
		}
		else if (charge > baseChargeDuration)
		{
			outer.SetNextState(GetNextStateAuthority());
		}
		if ((bool)chargeVfxInstanceTransform)
		{
			base.characterMotor.walkSpeedPenaltyCoefficient = walkSpeedCoefficient;
		}
	}

	public override void Update()
	{
		if (!stateAborted)
		{
			base.Update();
			Mathf.Clamp01(base.age / chargeDuration);
		}
	}

	protected virtual EntityState GetNextStateAuthority()
	{
		return new SwatAwayPlayersSlam();
	}
}
