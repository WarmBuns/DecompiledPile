using System;
using RoR2;
using UnityEngine;

namespace EntityStates.VoidRaidCrab;

public class BaseSpinBeamAttackState : BaseState
{
	[SerializeField]
	public string animLayerName;

	[SerializeField]
	public string animStateName;

	[SerializeField]
	public string animPlaybackRateParamName;

	[SerializeField]
	public float baseDuration;

	public static string headTransformNameInChildLocator;

	public static string muzzleTransformNameInChildLocator;

	public static string headYawParamName;

	[SerializeField]
	public AnimationCurve headForwardYCurve;

	private Transform headTransform;

	private Transform muzzleTransform;

	private bool hasHeadSpinOwnership;

	protected float duration { get; private set; }

	protected Animator modelAnimator { get; private set; }

	protected GameObject beamVfxInstance { get; private set; }

	protected float normalizedFixedAge => Mathf.Clamp01(base.fixedAge / duration);

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		headTransform = FindModelChild(headTransformNameInChildLocator);
		muzzleTransform = FindModelChild(muzzleTransformNameInChildLocator);
		duration = baseDuration;
		if (!string.IsNullOrEmpty(animLayerName) && !string.IsNullOrEmpty(animStateName))
		{
			if (!string.IsNullOrEmpty(animPlaybackRateParamName))
			{
				PlayAnimation(animLayerName, animStateName, animPlaybackRateParamName, duration);
			}
			else
			{
				PlayAnimation(animLayerName, animStateName);
			}
		}
		if ((bool)modelAnimator)
		{
			modelAnimator.GetComponent<AimAnimator>().enabled = true;
		}
		hasHeadSpinOwnership = true;
	}

	public override void ModifyNextState(EntityState nextState)
	{
		base.ModifyNextState(nextState);
		if (nextState is BaseSpinBeamAttackState baseSpinBeamAttackState)
		{
			baseSpinBeamAttackState.hasHeadSpinOwnership |= hasHeadSpinOwnership;
			hasHeadSpinOwnership = false;
		}
	}

	public override void OnExit()
	{
		if (hasHeadSpinOwnership)
		{
			SetHeadYawRevolutions(0f);
			hasHeadSpinOwnership = true;
		}
		DestroyBeamVFXInstance();
		base.OnExit();
	}

	protected void SetHeadYawRevolutions(float newRevolutions)
	{
		if ((bool)modelAnimator)
		{
			modelAnimator.SetFloat(headYawParamName, (0.5f + newRevolutions) % 1f);
		}
	}

	protected Ray GetBeamRay()
	{
		_ = muzzleTransform.forward;
		Vector3 forward = headTransform.forward;
		forward.y = headForwardYCurve.Evaluate(base.fixedAge / duration);
		forward.Normalize();
		return new Ray(muzzleTransform.position, forward);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	protected void CreateBeamVFXInstance(GameObject beamVfxPrefab)
	{
		if ((object)beamVfxInstance != null)
		{
			throw new InvalidOperationException();
		}
		beamVfxInstance = UnityEngine.Object.Instantiate(beamVfxPrefab);
		beamVfxInstance.transform.SetParent(headTransform, worldPositionStays: true);
		UpdateBeamTransforms();
		RoR2Application.onLateUpdate += UpdateBeamTransformsInLateUpdate;
	}

	protected void DestroyBeamVFXInstance()
	{
		if ((object)beamVfxInstance != null)
		{
			RoR2Application.onLateUpdate -= UpdateBeamTransformsInLateUpdate;
			VfxKillBehavior.KillVfxObject(beamVfxInstance);
			beamVfxInstance = null;
		}
	}

	private void UpdateBeamTransformsInLateUpdate()
	{
		try
		{
			UpdateBeamTransforms();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	private void UpdateBeamTransforms()
	{
		Ray beamRay = GetBeamRay();
		beamVfxInstance.transform.SetPositionAndRotation(beamRay.origin, Quaternion.LookRotation(beamRay.direction));
	}
}
