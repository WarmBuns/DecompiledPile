using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ClayBruiser.Weapon;

public class MinigunState : BaseState
{
	public static string muzzleName;

	private static readonly BuffDef slowBuff = RoR2Content.Buffs.Slow50;

	protected Transform muzzleTransform;

	protected ref InputBankTest.ButtonState skillButtonState => ref base.inputBank.skill1;

	public override void OnEnter()
	{
		base.OnEnter();
		muzzleTransform = FindModelChild(muzzleName);
		if (NetworkServer.active && (bool)base.characterBody)
		{
			base.characterBody.AddBuff(slowBuff);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		StartAimMode();
	}

	public override void OnExit()
	{
		if (NetworkServer.active && (bool)base.characterBody)
		{
			base.characterBody.RemoveBuff(slowBuff);
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
