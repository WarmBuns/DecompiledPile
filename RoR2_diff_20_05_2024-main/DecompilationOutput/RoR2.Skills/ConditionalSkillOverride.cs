using System;
using UnityEngine;

namespace RoR2.Skills;

public class ConditionalSkillOverride : MonoBehaviour
{
	[Serializable]
	public struct ConditionalSkillInfo
	{
		public GenericSkill skillSlot;

		public SkillDef sprintSkillDef;

		public SkillDef airborneSkillDef;
	}

	public CharacterBody characterBody;

	public ConditionalSkillInfo[] conditionalSkillInfos;

	private bool wasSprinting;

	private bool wasAirborne;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		bool flag = false;
		bool flag2 = false;
		if ((bool)characterBody)
		{
			flag = characterBody.isSprinting;
			if ((bool)characterBody.characterMotor)
			{
				flag2 = characterBody.characterMotor.isGrounded;
			}
		}
		bool flag3 = wasSprinting || wasAirborne;
		bool flag4 = flag || flag2;
		ConditionalSkillInfo[] array = conditionalSkillInfos;
		for (int i = 0; i < array.Length; i++)
		{
			ConditionalSkillInfo conditionalSkillInfo = array[i];
			if (flag3)
			{
				if (wasAirborne && !flag2)
				{
					conditionalSkillInfo.skillSlot.UnsetSkillOverride(this, conditionalSkillInfo.airborneSkillDef, GenericSkill.SkillOverridePriority.Contextual);
				}
				else if (wasSprinting && !flag)
				{
					conditionalSkillInfo.skillSlot.UnsetSkillOverride(this, conditionalSkillInfo.sprintSkillDef, GenericSkill.SkillOverridePriority.Contextual);
				}
			}
			if (flag4)
			{
				if (flag2 && !wasAirborne)
				{
					conditionalSkillInfo.skillSlot.SetSkillOverride(this, conditionalSkillInfo.airborneSkillDef, GenericSkill.SkillOverridePriority.Contextual);
				}
				else if (flag && !wasSprinting)
				{
					conditionalSkillInfo.skillSlot.SetSkillOverride(this, conditionalSkillInfo.sprintSkillDef, GenericSkill.SkillOverridePriority.Contextual);
				}
			}
		}
		wasAirborne = flag2;
		wasSprinting = flag;
	}
}
