using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;

namespace EntityStates.SpectatorSkills;

public class LockOn : BaseSkillState
{
	private Vector3 targetPoint;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.inputBank.GetAimRaycast(float.PositiveInfinity, out var hitInfo))
		{
			targetPoint = hitInfo.point;
		}
		else
		{
			outer.SetNextStateToMain();
		}
		RoR2Application.onUpdate += LookAtTarget;
		RoR2Application.onLateUpdate += LookAtTarget;
	}

	public override void OnExit()
	{
		RoR2Application.onLateUpdate -= LookAtTarget;
		RoR2Application.onUpdate -= LookAtTarget;
		base.OnExit();
	}

	public override void Update()
	{
		base.Update();
		if (base.isAuthority && !IsKeyDownAuthority())
		{
			outer.SetNextStateToMain();
		}
	}

	private void LookAtTarget()
	{
		ReadOnlyCollection<CameraRigController> readOnlyInstancesList = CameraRigController.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			_ = readOnlyInstancesList[i].target == base.gameObject;
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
