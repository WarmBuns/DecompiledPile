using RoR2;
using UnityEngine;

namespace EntityStates.UrchinTurret.Weapon;

public class MinigunState : BaseState
{
	public static string muzzleName;

	protected Transform muzzleTransform;

	protected ref InputBankTest.ButtonState skillButtonState => ref base.inputBank.skill1;

	public override void OnEnter()
	{
		base.OnEnter();
		muzzleTransform = FindModelChild(muzzleName);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		StartAimMode();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
