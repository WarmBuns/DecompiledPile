using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace EntityStates;

public class SkillStateOverrideData
{
	private object source;

	public bool duplicateStock;

	public bool overrideFullReloadOnAssign;

	public bool simulateRestockForOverridenSkills = true;

	private static GenericSkill.SkillOverridePriority priority = GenericSkill.SkillOverridePriority.Contextual;

	private float overrideTimestamp;

	public SkillDef primarySkillOverride;

	public SkillDef secondarySkillOverride;

	public SkillDef utilitySkillOverride;

	public SkillDef specialSkillOverride;

	private GenericSkill primarySkillOriginal;

	private GenericSkill secondarySkillOriginal;

	private GenericSkill utilitySkillOriginal;

	private GenericSkill specialSkillOriginal;

	public int previousPrimaryStock = -1;

	public int previousSecondaryStock = -1;

	public int previousUtilityStock = -1;

	public int previousSpecialStock = -1;

	public bool hasAuthority { get; private set; }

	public SkillStateOverrideData(CharacterBody sourceBody)
	{
		source = sourceBody;
		hasAuthority = Util.HasEffectiveAuthority(sourceBody.gameObject);
		duplicateStock = false;
	}

	public SkillStateOverrideData(CharacterBody sourceBody, bool _duplicateStock)
	{
		source = sourceBody;
		hasAuthority = Util.HasEffectiveAuthority(sourceBody.gameObject);
		duplicateStock = _duplicateStock;
	}

	public void OverrideSkills(SkillLocator skillLocator)
	{
		if (hasAuthority && !(skillLocator == null))
		{
			overrideTimestamp = Time.time;
			InternalOverride(skillLocator.primary, ref primarySkillOverride, ref primarySkillOriginal, ref previousPrimaryStock);
			InternalOverride(skillLocator.secondary, ref secondarySkillOverride, ref secondarySkillOriginal, ref previousSecondaryStock);
			InternalOverride(skillLocator.utility, ref utilitySkillOverride, ref utilitySkillOriginal, ref previousUtilityStock);
			InternalOverride(skillLocator.special, ref specialSkillOverride, ref specialSkillOriginal, ref previousSpecialStock);
		}
	}

	private void InternalOverride(GenericSkill existingSkillToOverride, ref SkillDef overridingSkill, ref GenericSkill originalSkill, ref int previousStockAmount)
	{
		if (!(overridingSkill == null) && !(existingSkillToOverride == null) && !(existingSkillToOverride == originalSkill) && !existingSkillToOverride.HasSkillOverrideOfPriority(priority))
		{
			int stock = existingSkillToOverride.stock;
			previousStockAmount = -1;
			originalSkill = existingSkillToOverride;
			originalSkill.SetSkillOverride(source, overridingSkill, priority);
			if (duplicateStock)
			{
				originalSkill.stock = stock;
			}
		}
	}

	public void ClearOverrides()
	{
		if (hasAuthority && source != null)
		{
			InternalClearOverride(ref primarySkillOriginal, ref primarySkillOverride, ref previousPrimaryStock);
			InternalClearOverride(ref secondarySkillOriginal, ref secondarySkillOverride, ref previousSecondaryStock);
			InternalClearOverride(ref utilitySkillOriginal, ref utilitySkillOverride, ref previousUtilityStock);
			InternalClearOverride(ref specialSkillOriginal, ref specialSkillOverride, ref previousSpecialStock);
		}
		void InternalClearOverride(ref GenericSkill overriddenSkill, ref SkillDef skillDef, ref int previousStockAmount)
		{
			if (!(overriddenSkill == null) && !(skillDef == null) && overriddenSkill.HasSkillOverrideOfPriority(priority))
			{
				int baseStock = overriddenSkill.GetBaseStock();
				int stock = overriddenSkill.stock;
				float rechargeStopwatch = overriddenSkill.rechargeStopwatch;
				overriddenSkill.UnsetSkillOverride(source, skillDef, priority);
				if (overrideFullReloadOnAssign && overriddenSkill.skillDef.fullRestockOnAssign)
				{
					overriddenSkill.stock = baseStock;
				}
				if (simulateRestockForOverridenSkills)
				{
					overriddenSkill.RechargeBaseSkill(Time.time - overrideTimestamp);
				}
				previousStockAmount = -1;
				if (duplicateStock)
				{
					overriddenSkill.baseStock = stock;
					overriddenSkill.baseRechargeStopwatch = rechargeStopwatch;
				}
			}
		}
	}

	public void StepRestock()
	{
		float time2 = Time.time;
		if (!(time2 <= overrideTimestamp))
		{
			InternalRestock(time2, ref primarySkillOriginal);
			InternalRestock(time2, ref secondarySkillOriginal);
			InternalRestock(time2, ref utilitySkillOriginal);
			InternalRestock(time2, ref specialSkillOriginal);
			overrideTimestamp = time2;
		}
		void InternalRestock(float time, ref GenericSkill overridenSkill)
		{
			if (!(overridenSkill == null))
			{
				overridenSkill.RechargeBaseSkill(time - overrideTimestamp);
			}
		}
	}
}
