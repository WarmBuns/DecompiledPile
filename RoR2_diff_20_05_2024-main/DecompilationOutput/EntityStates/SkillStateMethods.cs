using System;
using RoR2;
using UnityEngine.Networking;

namespace EntityStates;

public static class SkillStateMethods
{
	public static void Serialize(this ISkillState skillState, SkillLocator skillLocator, NetworkWriter writer)
	{
		int num = -1;
		if ((object)skillLocator != null)
		{
			num = skillLocator.GetSkillSlotIndex(skillState.activatorSkillSlot);
		}
		writer.Write((sbyte)num);
	}

	public static void Deserialize(this ISkillState skillState, SkillLocator skillLocator, NetworkReader reader)
	{
		int index = reader.ReadSByte();
		if ((object)skillLocator != null)
		{
			skillState.activatorSkillSlot = skillLocator.GetSkillAtIndex(index);
		}
	}

	public static bool IsKeyDownAuthority(this ISkillState skillState, SkillLocator skillLocator, InputBankTest inputBank)
	{
		GenericSkill activatorSkillSlot = skillState.activatorSkillSlot;
		if ((object)skillLocator == null || (object)activatorSkillSlot == null || (object)inputBank == null)
		{
			return false;
		}
		return skillLocator.FindSkillSlot(activatorSkillSlot) switch
		{
			SkillSlot.None => false, 
			SkillSlot.Primary => inputBank.skill1.down, 
			SkillSlot.Secondary => inputBank.skill2.down, 
			SkillSlot.Utility => inputBank.skill3.down, 
			SkillSlot.Special => inputBank.skill4.down, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}
}
