using RoR2;
using UnityEngine;

namespace EntityStates.Toolbot;

public static class BaseToolbotPrimarySkillStateMethods
{
	private static int DualWieldFireLStateHash = Animator.StringToHash("DualWieldFire, Left");

	private static int DualWieldFireRStateHash = Animator.StringToHash("DualWieldFire, Right");

	public static void OnEnter<T>(T state, GameObject gameObject, SkillLocator skillLocator, ChildLocator modelChildLocator) where T : BaseState, IToolbotPrimarySkillState
	{
		state.currentHand = 0;
		state.isInDualWield = EntityStateMachine.FindByCustomName(gameObject, "Body").state is ToolbotDualWield;
		state.muzzleName = state.baseMuzzleName;
		state.skillDef = state.activatorSkillSlot.skillDef;
		if (state.isInDualWield)
		{
			if ((object)state.activatorSkillSlot == skillLocator.primary)
			{
				state.currentHand = -1;
				state.muzzleName = "DualWieldMuzzleL";
			}
			else if ((object)state.activatorSkillSlot == skillLocator.secondary)
			{
				state.currentHand = 1;
				state.muzzleName = "DualWieldMuzzleR";
			}
		}
		if (state.muzzleName != null)
		{
			state.muzzleTransform = modelChildLocator.FindChild(state.muzzleName);
		}
	}

	public static void PlayGenericFireAnim<T>(T state, GameObject gameObject, SkillLocator skillLocator, float duration) where T : BaseState, IToolbotPrimarySkillState
	{
		state.currentHand = 0;
		if ((object)state.activatorSkillSlot == skillLocator.primary)
		{
			state.currentHand = -1;
		}
		else if ((object)state.activatorSkillSlot == skillLocator.secondary)
		{
			state.currentHand = 1;
		}
		switch (state.currentHand)
		{
		case -1:
			state.PlayAnimation("Gesture, Additive", DualWieldFireLStateHash);
			break;
		case 1:
			state.PlayAnimation("Gesture, Additive Bonus", DualWieldFireRStateHash);
			break;
		}
	}
}
