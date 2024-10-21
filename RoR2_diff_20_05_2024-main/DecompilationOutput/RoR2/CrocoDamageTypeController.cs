using RoR2.Skills;
using UnityEngine;

namespace RoR2;

public class CrocoDamageTypeController : MonoBehaviour
{
	public SkillDef poisonSkillDef;

	public SkillDef blightSkillDef;

	public GenericSkill passiveSkillSlot;

	public DamageTypeCombo GetDamageType()
	{
		if ((bool)passiveSkillSlot)
		{
			if (passiveSkillSlot.skillDef == poisonSkillDef)
			{
				return DamageType.PoisonOnHit;
			}
			if (passiveSkillSlot.skillDef == blightSkillDef)
			{
				return DamageType.BlightOnHit;
			}
		}
		return DamageType.Generic;
	}
}
