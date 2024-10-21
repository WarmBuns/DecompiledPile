using System;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.FalseSon;

public class ClubSwing3 : BaseSkillState, ISkillOverrideHandoff
{
	private class ArcVisualizer : IDisposable
	{
		private readonly Vector3[] points;

		private readonly float duration;

		private readonly GameObject arcVisualizerInstance;

		private readonly LineRenderer lineRenderer;

		public ArcVisualizer(GameObject arcVisualizerPrefab, float duration, int vertexCount)
		{
			arcVisualizerInstance = UnityEngine.Object.Instantiate(arcVisualizerPrefab);
			lineRenderer = arcVisualizerInstance.GetComponent<LineRenderer>();
			lineRenderer.positionCount = vertexCount;
			points = new Vector3[vertexCount];
			this.duration = duration;
		}

		public void Dispose()
		{
			EntityState.Destroy(arcVisualizerInstance);
		}

		public void SetParameters(Vector3 origin, Vector3 initialVelocity, float characterMaxSpeed, float characterAcceleration)
		{
			arcVisualizerInstance.transform.position = origin;
			if (!lineRenderer.useWorldSpace)
			{
				Vector3 eulerAngles = Quaternion.LookRotation(initialVelocity).eulerAngles;
				eulerAngles.x = 0f;
				eulerAngles.z = 0f;
				Quaternion rotation = Quaternion.Euler(eulerAngles);
				arcVisualizerInstance.transform.rotation = rotation;
				origin = Vector3.zero;
				initialVelocity = Quaternion.Inverse(rotation) * initialVelocity;
			}
			else
			{
				arcVisualizerInstance.transform.rotation = Quaternion.LookRotation(Vector3.Cross(initialVelocity, Vector3.up));
			}
			float y = Physics.gravity.y;
			float num = duration / (float)points.Length;
			Vector3 vector = origin;
			Vector3 vector2 = initialVelocity;
			float num2 = num;
			float num3 = y * num2;
			float maxDistanceDelta = characterAcceleration * num2;
			for (int i = 0; i < points.Length; i++)
			{
				points[i] = vector;
				Vector2 current = Util.Vector3XZToVector2XY(vector2);
				current = Vector2.MoveTowards(current, Vector3.zero, maxDistanceDelta);
				vector2.x = current.x;
				vector2.z = current.y;
				vector2.y += num3;
				vector += vector2 * num2;
			}
			lineRenderer.SetPositions(points);
		}
	}

	public static GameObject arcVisualizerPrefab;

	public static float arcVisualizerSimulationLength;

	public static int arcVisualizerVertexCount;

	[SerializeField]
	public float baseChargeDuration = 1f;

	[SerializeField]
	public float minChargeToDisplayAttackCharging;

	[SerializeField]
	public float minChargeToChargeAttack;

	[SerializeField]
	public float cooldownOnCancelDuration = 1f;

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

	private SkillStateOverrideData skillOverrideData;

	private bool clearOverrides = true;

	private bool nonAuthority_AllowEarlyCancelAnimationToFinish;

	private float cooldownTimestamp = -1f;

	protected float chargeDuration { get; private set; }

	protected float charge { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		chargeDuration = baseChargeDuration / attackSpeedStat;
		Util.PlaySound(enterSFXString, base.gameObject);
		soundID = Util.PlaySound(startChargeLoopSFXString, base.gameObject);
		clearOverrides = true;
		ResetPrimarySwingCount();
	}

	private void ResetPrimarySwingCount()
	{
		SteppedSkillDef.InstanceData instanceData = (SteppedSkillDef.InstanceData)(base.skillLocator.GetSkill(SkillSlot.Primary)?.skillInstanceData);
		if (instanceData != null)
		{
			instanceData.step = 0;
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	public override void FixedUpdate()
	{
		if (skillOverrideData != null)
		{
			skillOverrideData.StepRestock();
		}
		if (cooldownTimestamp != -1f)
		{
			if (!(Time.time - cooldownTimestamp < cooldownOnCancelDuration))
			{
				outer.SetNextStateToMain();
			}
			return;
		}
		base.FixedUpdate();
		charge = Mathf.Clamp01(base.fixedAge / chargeDuration);
		AkSoundEngine.SetRTPCValueByPlayingID("charFalseSon_skill1_chargeAmount", charge * 100f, soundID);
		base.characterBody.SetSpreadBloom(charge);
		base.characterBody.SetAimTimer(3f);
		if (charge >= minChargeToDisplayAttackCharging && !chargeVfxInstanceTransform && (bool)chargeVfxPrefab)
		{
			if ((bool)crosshairOverridePrefab && crosshairOverrideRequest == null)
			{
				crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
			}
			Transform transform = FindModelChild(chargeVfxChildLocatorName);
			if ((bool)transform && charge > minChargeToChargeAttack)
			{
				chargeVfxInstanceTransform = EffectManager.GetAndActivatePooledEffect(chargeVfxPrefab, transform, inResetLocal: true)?.transform;
				ScaleParticleSystemDuration component = chargeVfxInstanceTransform.GetComponent<ScaleParticleSystemDuration>();
				if ((bool)component)
				{
					component.newDuration = (1f - minChargeToDisplayAttackCharging) * chargeDuration;
				}
			}
			if (!chargeAnimPlayed)
			{
				PlayAnimation("Gesture, Additive", "ChargeSwingIntro", "ChargeSwingIntro.playbackRate", 0.2f * chargeDuration, 0.1f);
				PlayAnimation("Gesture, Override", "ChargeSwingIntro", "ChargeSwingIntro.playbackRate", 0.2f * chargeDuration, 0.1f);
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
		if (!base.inputBank.skill1.down && !base.inputBank.skill2.down)
		{
			if ((base.inputBank.skill2.justReleased || base.inputBank.skill1.justReleased) && CanOverheadSwing())
			{
				outer.SetNextState(GetNextStateAuthority());
			}
			else
			{
				HandleEarlyCancel();
			}
		}
	}

	private void HandleEarlyCancel()
	{
		if (cooldownTimestamp == -1f)
		{
			PlayAnimation("Gesture, Additive", "ChargeSwingExit");
			PlayAnimation("Gesture, Override", "ChargeSwingExit");
			cooldownTimestamp = Time.time;
		}
	}

	private bool CanOverheadSwing()
	{
		if (!(charge > minChargeToChargeAttack))
		{
			return base.isGrounded;
		}
		return true;
	}

	protected virtual bool ShouldKeepChargingAuthority()
	{
		return IsKeyDownAuthority();
	}

	protected virtual EntityState GetNextStateAuthority()
	{
		if (!base.isGrounded)
		{
			return new PreClubGroundSlam
			{
				charge = charge
			};
		}
		if (charge > minChargeToChargeAttack)
		{
			return new ChargedClubSwing
			{
				charge = charge,
				chargeMetMinimumHold = (charge > minChargeToChargeAttack)
			};
		}
		return new OverheadClubSwing();
	}

	public override void OnExit()
	{
		if (clearOverrides)
		{
			skillOverrideData?.ClearOverrides();
		}
		if ((bool)chargeVfxInstanceTransform)
		{
			EntityState.Destroy(chargeVfxInstanceTransform.gameObject);
			crosshairOverrideRequest?.Dispose();
			chargeVfxInstanceTransform = null;
		}
		if (!nonAuthority_AllowEarlyCancelAnimationToFinish)
		{
			PlayAnimation("Gesture, Additive", "Empty");
			PlayAnimation("Gesture, Override", "Empty");
		}
		base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
		Util.PlaySound(endChargeLoopSFXString, base.gameObject);
		base.OnExit();
	}

	public override void ModifyNextState(EntityState nextState)
	{
		base.ModifyNextState(nextState);
		if (nextState is ISkillOverrideHandoff skillOverrideHandoff)
		{
			skillOverrideHandoff.TransferSkillOverride(skillOverrideData);
			clearOverrides = false;
			if (!base.isAuthority && nextState is IdleSkillOverrideHandoff)
			{
				HandleEarlyCancel();
				nonAuthority_AllowEarlyCancelAnimationToFinish = true;
			}
		}
	}

	public void TransferSkillOverride(SkillStateOverrideData skillOverrideData)
	{
		this.skillOverrideData = skillOverrideData;
	}
}
