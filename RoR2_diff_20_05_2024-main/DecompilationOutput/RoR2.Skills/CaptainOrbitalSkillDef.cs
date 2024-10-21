using UnityEngine;

namespace RoR2.Skills;

public class CaptainOrbitalSkillDef : SkillDef
{
	public Sprite disabledIcon;

	public string disabledNameToken;

	public string disabledDescriptionToken;

	private bool isAvailable => !SceneCatalog.mostRecentSceneDef.blockOrbitalSkills;

	public override Sprite GetCurrentIcon(GenericSkill skillSlot)
	{
		if (!isAvailable)
		{
			return disabledIcon;
		}
		return base.GetCurrentIcon(skillSlot);
	}

	public override string GetCurrentNameToken(GenericSkill skillSlot)
	{
		if (!isAvailable)
		{
			return disabledNameToken;
		}
		return base.GetCurrentNameToken(skillSlot);
	}

	public override string GetCurrentDescriptionToken(GenericSkill skillSlot)
	{
		if (!isAvailable)
		{
			return disabledDescriptionToken;
		}
		return base.GetCurrentDescriptionToken(skillSlot);
	}

	public override bool CanExecute(GenericSkill skillSlot)
	{
		if (isAvailable)
		{
			return base.CanExecute(skillSlot);
		}
		return false;
	}

	public override bool IsReady(GenericSkill skillSlot)
	{
		if (isAvailable)
		{
			return base.IsReady(skillSlot);
		}
		return false;
	}
}
