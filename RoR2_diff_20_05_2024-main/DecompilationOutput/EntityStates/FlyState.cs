using RoR2;
using UnityEngine;

namespace EntityStates;

public class FlyState : BaseState
{
	private Animator modelAnimator;

	private bool skill1InputReceived;

	private bool skill2InputReceived;

	private bool skill3InputReceived;

	private bool skill4InputReceived;

	private static int IdleStateHash = Animator.StringToHash("Idle");

	private bool hasPivotPitchLayer;

	private bool hasPivotYawLayer;

	private bool hasPivotRollLayer;

	private static readonly int pivotPitchCycle = Animator.StringToHash("pivotPitchCycle");

	private static readonly int pivotYawCycle = Animator.StringToHash("pivotYawCycle");

	private static readonly int pivotRollCycle = Animator.StringToHash("pivotRollCycle");

	private static readonly int flyRate = Animator.StringToHash("fly.rate");

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		PlayAnimation("Body", IdleStateHash);
		if ((bool)modelAnimator)
		{
			hasPivotPitchLayer = modelAnimator.GetLayerIndex("PivotPitch") != -1;
			hasPivotYawLayer = modelAnimator.GetLayerIndex("PivotYaw") != -1;
			hasPivotRollLayer = modelAnimator.GetLayerIndex("PivotRoll") != -1;
		}
	}

	public override void Update()
	{
		base.Update();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)base.rigidbodyDirection)
		{
			Quaternion rotation = base.transform.rotation;
			Quaternion quaternion = Util.QuaternionSafeLookRotation(base.rigidbodyDirection.aimDirection);
			Quaternion quaternion2 = Quaternion.Inverse(rotation) * quaternion;
			if ((bool)modelAnimator)
			{
				float deltaTime = GetDeltaTime();
				if (hasPivotPitchLayer)
				{
					modelAnimator.SetFloat(pivotPitchCycle, Mathf.Clamp01(Util.Remap(quaternion2.x * Mathf.Sign(quaternion2.w), -1f, 1f, 0f, 1f)), 1f, deltaTime);
				}
				if (hasPivotYawLayer)
				{
					modelAnimator.SetFloat(pivotYawCycle, Mathf.Clamp01(Util.Remap(quaternion2.y * Mathf.Sign(quaternion2.w), -1f, 1f, 0f, 1f)), 1f, deltaTime);
				}
				if (hasPivotRollLayer)
				{
					modelAnimator.SetFloat(pivotRollCycle, Mathf.Clamp01(Util.Remap(quaternion2.z * Mathf.Sign(quaternion2.w), -1f, 1f, 0f, 1f)), 1f, deltaTime);
				}
			}
		}
		PerformInputs();
	}

	protected virtual bool CanExecuteSkill(GenericSkill skillSlot)
	{
		return true;
	}

	protected virtual void PerformInputs()
	{
		if (!base.isAuthority)
		{
			return;
		}
		if ((bool)base.inputBank)
		{
			if ((bool)base.rigidbodyMotor)
			{
				base.rigidbodyMotor.moveVector = base.inputBank.moveVector * base.characterBody.moveSpeed;
				if ((bool)modelAnimator)
				{
					modelAnimator.SetFloat(flyRate, Vector3.Magnitude(base.rigidbodyMotor.rigid.velocity));
				}
			}
			if ((bool)base.rigidbodyDirection)
			{
				base.rigidbodyDirection.aimDirection = GetAimRay().direction;
			}
			skill1InputReceived = base.inputBank.skill1.down;
			skill2InputReceived = base.inputBank.skill2.down;
			skill3InputReceived = base.inputBank.skill3.down;
			skill4InputReceived = base.inputBank.skill4.down;
		}
		if ((bool)base.skillLocator)
		{
			if (skill1InputReceived && (bool)base.skillLocator.primary && CanExecuteSkill(base.skillLocator.primary))
			{
				base.skillLocator.primary.ExecuteIfReady();
			}
			if (skill2InputReceived && (bool)base.skillLocator.secondary && CanExecuteSkill(base.skillLocator.secondary))
			{
				base.skillLocator.secondary.ExecuteIfReady();
			}
			if (skill3InputReceived && (bool)base.skillLocator.utility && CanExecuteSkill(base.skillLocator.utility))
			{
				base.skillLocator.utility.ExecuteIfReady();
			}
			if (skill4InputReceived && (bool)base.skillLocator.special && CanExecuteSkill(base.skillLocator.special))
			{
				base.skillLocator.special.ExecuteIfReady();
			}
		}
	}
}
