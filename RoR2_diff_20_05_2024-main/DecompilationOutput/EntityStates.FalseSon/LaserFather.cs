using RoR2;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.FalseSon;

public class LaserFather : BaseSkillState
{
	[SerializeField]
	public float baseChargeDuration = 1f;

	[SerializeField]
	public GameObject chargeCompletePrefab;

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

	public static float baseDuration = 3f;

	public static float laserMaxWidth = 0.2f;

	public static GameObject effectPrefab;

	public static GameObject laserPrefab;

	public static string attackSoundString;

	private float duration;

	private uint chargePlayID;

	private GameObject chargeEffect;

	private GameObject laserEffect;

	private LineRenderer laserLineComponent;

	private Vector3 laserDirection;

	private Vector3 visualEndPosition;

	private float flashTimer;

	private bool laserOn;

	private bool chargeAnimPlayed;

	private bool chargeFinished;

	private bool chargeStarted;

	private bool cancellingAttack;

	protected float chargeDuration { get; private set; }

	protected float charge { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		chargeDuration = baseChargeDuration / attackSpeedStat;
		soundID = Util.PlaySound(startChargeLoopSFXString, base.gameObject);
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		chargePlayID = Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSpeedStat);
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("MuzzleLaser");
				if ((bool)transform)
				{
					if ((bool)effectPrefab)
					{
						chargeEffect = Object.Instantiate(effectPrefab, transform.position, transform.rotation);
						chargeEffect.transform.parent = transform;
						ScaleParticleSystemDuration component2 = chargeEffect.GetComponent<ScaleParticleSystemDuration>();
						if ((bool)component2)
						{
							component2.newDuration = duration;
						}
					}
					if ((bool)laserPrefab)
					{
						laserEffect = Object.Instantiate(laserPrefab, transform.position, transform.rotation);
						laserEffect.transform.parent = transform;
						laserLineComponent = laserEffect.GetComponent<LineRenderer>();
					}
				}
			}
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
		flashTimer = 0f;
		laserOn = true;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	public override void ModifyNextState(EntityState nextState)
	{
		base.ModifyNextState(nextState);
		LaserFatherCharged laserFatherCharged = nextState as LaserFatherCharged;
		cancellingAttack = laserFatherCharged == null;
	}

	public override void OnExit()
	{
		if ((bool)chargeVfxInstanceTransform)
		{
			EntityState.Destroy(chargeVfxInstanceTransform.gameObject);
			crosshairOverrideRequest?.Dispose();
			chargeVfxInstanceTransform = null;
		}
		PlayAnimation("Gesture, Additive", "Empty");
		PlayAnimation("Gesture, Override", "Empty");
		if (cancellingAttack)
		{
			PlayAnimation("Gesture, Head, Additive", "Empty");
			PlayAnimation("Gesture, Head, Override", "Buffer Empty");
		}
		base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
		Util.PlaySound(endChargeLoopSFXString, base.gameObject);
		AkSoundEngine.StopPlayingID(chargePlayID);
		Util.PlaySound("Stop_falseson_skill4_laser_loop", base.gameObject);
		Util.PlaySound("Stop_falseson_skill4_laser_charge_loop", base.gameObject);
		base.OnExit();
		if ((bool)chargeEffect)
		{
			EntityState.Destroy(chargeEffect);
		}
		if ((bool)laserEffect)
		{
			EntityState.Destroy(laserEffect);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		charge = Mathf.Clamp01(base.fixedAge / chargeDuration);
		AkSoundEngine.SetRTPCValueByPlayingID("charFalseSon_skill4_chargeAmount", charge * 100f, soundID);
		base.characterBody.SetSpreadBloom(charge);
		base.characterBody.SetAimTimer(3f);
		if (charge >= minChargeForChargedAttack && !chargeStarted)
		{
			chargeStarted = true;
			base.characterMotor.walkSpeedPenaltyCoefficient = walkSpeedCoefficient;
			PlayCrossfade("Gesture, Head, Additive", "LaserChargeIntro", "LaserChargeIntro.playbackRate", 0.2f * chargeDuration, 0.1f);
			PlayCrossfade("Gesture, Head, Override", "LaserChargeIntro", "LaserChargeIntro.playbackRate", 0.2f * chargeDuration, 0.1f);
		}
		if (base.isAuthority)
		{
			AuthorityFixedUpdate();
		}
		if (!(charge > 0.85f) || chargeFinished)
		{
			return;
		}
		ChildLocator component = GetModelTransform().GetComponent<ChildLocator>();
		if ((bool)component)
		{
			Transform transform = component.FindChild("MuzzleLaser");
			if ((bool)transform)
			{
				chargeCompletePrefab = LegacyResourcesAPI.Load<GameObject>("FalseSon/FalseSonLaserChargeComplete");
				EffectManager.SpawnEffect(chargeCompletePrefab, new EffectData
				{
					origin = transform.transform.position,
					scale = 1f
				}, transmit: true);
				chargeFinished = true;
			}
		}
	}

	public override void Update()
	{
		base.Update();
		Mathf.Clamp01(base.age / chargeDuration);
	}

	private void AuthorityFixedUpdate()
	{
		if (!ShouldKeepChargingAuthority())
		{
			outer.SetNextState(GetNextStateAuthority());
		}
	}

	protected virtual bool ShouldKeepChargingAuthority()
	{
		return IsKeyDownAuthority();
	}

	protected virtual EntityState GetNextStateAuthority()
	{
		if (charge < 0.175f)
		{
			charge = 0.175f;
		}
		return new LaserFatherCharged
		{
			charge = charge,
			walkSpeedCoefficient = walkSpeedCoefficient
		};
	}
}
